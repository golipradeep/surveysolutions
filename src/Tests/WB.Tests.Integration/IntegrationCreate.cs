﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Humanizer;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Main.Core.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Ncqrs.Eventing;
using Ncqrs.Eventing.ServiceModel.Bus;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Tool.hbm2ddl;
using Npgsql;
using WB.Core.BoundedContexts.Designer.CodeGenerationV2;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration;
using WB.Core.BoundedContexts.Designer.Implementation.Services.LookupTableService;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Translations;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Preloading;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Workspaces;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.Infrastructure.Aggregates;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.CommandBus.Implementation;
using WB.Core.Infrastructure.Domain;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.Infrastructure.Services;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities.Answers;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;
using WB.Core.SharedKernels.SurveySolutions;
using WB.Core.SharedKernels.SurveySolutions.Documents;
using WB.Infrastructure.Native.Storage.Postgre;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;
using IEvent = WB.Core.Infrastructure.EventBus.IEvent;
using WB.Infrastructure.Native.Storage;
using WB.Infrastructure.Native.Storage.Postgre.NhExtensions;
using WB.Infrastructure.Native.Workspaces;
using WB.Tests.Abc;
using WB.UI.Headquarters;
using Configuration = NHibernate.Cfg.Configuration;
using Environment = System.Environment;

namespace WB.Tests.Integration
{
    internal static class IntegrationCreate
    {
        public static string CompileAssembly(QuestionnaireDocument questionnaireDocument)
        {
            var expressionProcessorGenerator =
                new QuestionnaireExpressionProcessorGenerator(
                    new RoslynCompiler(),
                    CodeGeneratorV2(),
                    new DynamicCompilerSettingsProvider());

            var latestSupportedVersion = DesignerEngineVersionService().LatestSupportedVersion;
            var emitResult = 
                expressionProcessorGenerator.GenerateProcessorStateAssembly(questionnaireDocument,  
                    latestSupportedVersion, 
                    out var resultAssembly);

            if (!emitResult.Success || string.IsNullOrEmpty(resultAssembly))
                throw new Exception(
                    $"Errors on IInterviewExpressionState generation:{Environment.NewLine}"
                    + string.Join(Environment.NewLine, emitResult.Diagnostics.Select((d, i) => $"{i + 1}. {d.Message}")));
            return resultAssembly;
        }

        public static CodeGeneratorV2 CodeGeneratorV2()
        {
            return new CodeGeneratorV2(CodeGenerationModelsFactory());
        }

        public static CodeGenerationModelsFactory CodeGenerationModelsFactory()
        {
            return new CodeGenerationModelsFactory(
                    DefaultMacrosSubstitutionService(),
                    ServiceLocator.Current.GetInstance<ILookupTableService>(),
                    new QuestionTypeToCSharpTypeMapper());
        }

        public static ExpressionsPlayOrderProvider ExpressionsPlayOrderProvider(
            IExpressionProcessor expressionProcessor = null,
            IMacrosSubstitutionService macrosSubstitutionService = null)
        {
            return new ExpressionsPlayOrderProvider(
                new ExpressionsGraphProvider(
                expressionProcessor ?? ServiceLocator.Current.GetInstance<IExpressionProcessor>(),
                macrosSubstitutionService ?? DefaultMacrosSubstitutionService()));
        }

        public static IMacrosSubstitutionService DefaultMacrosSubstitutionService()
        {
            var macrosSubstitutionServiceMock = new Mock<IMacrosSubstitutionService>();
            macrosSubstitutionServiceMock.Setup(
                x => x.InlineMacros(It.IsAny<string>(), It.IsAny<IEnumerable<Macro>>()))
                .Returns((string e, IEnumerable<Macro> macros) =>
                {
                    return e;
                });

            return macrosSubstitutionServiceMock.Object;
        }

        public static Interview Interview(
            Guid? questionnaireId = null,
            IQuestionnaireStorage questionnaireRepository = null, 
            IInterviewExpressionStorageProvider expressionProcessorStorageProvider = null,
            IQuestionOptionsRepository questionOptionsRepository = null)
        {

            var serviceLocator = new Mock<IServiceLocator>();

            var qRepository = questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>();
            serviceLocator.Setup(x => x.GetInstance<IQuestionnaireStorage>())
                .Returns(qRepository);

            var expressionsProvider = expressionProcessorStorageProvider ?? Mock.Of<IInterviewExpressionStorageProvider>();
            serviceLocator.Setup(x => x.GetInstance<IInterviewExpressionStorageProvider>())
                .Returns(expressionsProvider);

            var optionsRepository = questionOptionsRepository ?? Mock.Of<IQuestionOptionsRepository>();
            serviceLocator.Setup(x => x.GetInstance<IQuestionOptionsRepository>())
                .Returns(optionsRepository);

            var interview = new Interview(
                Create.Service.SubstitutionTextFactory(),
                Create.Service.InterviewTreeBuilder(),
                optionsRepository);

            interview.ServiceLocatorInstance = serviceLocator.Object;

            interview.CreateInterview(Create.Command.CreateInterview(
                interviewId: interview.EventSourceId, 
                userId: new Guid("F111F111F111F111F111F111F111F111"),
                questionnaireId: questionnaireId ?? new Guid("B000B000B000B000B000B000B000B000"),
                version: 1,
                answers:new List<InterviewAnswer>(),
                supervisorId: new Guid("D222D222D222D222D222D222D222D222")));

            return interview;
        }

        public static StatefulInterview PreloadedInterview(
            PreloadedDataDto preloadedData,
            Guid? questionnaireId = null,
            IQuestionnaireStorage questionnaireRepository = null, 
            IInterviewExpressionStorageProvider expressionProcessorStorageProvider = null)
        {
            var serviceLocator = new Mock<IServiceLocator>();

            var qRepository = questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>();
            serviceLocator.Setup(x => x.GetInstance<IQuestionnaireStorage>())
                .Returns(qRepository);

            var expressionsProvider = expressionProcessorStorageProvider ?? Mock.Of<IInterviewExpressionStorageProvider>();
            serviceLocator.Setup(x => x.GetInstance<IInterviewExpressionStorageProvider>())
                .Returns(expressionsProvider);

            var optionsRepository = Mock.Of<IQuestionOptionsRepository>();
            serviceLocator.Setup(x => x.GetInstance<IQuestionOptionsRepository>())
                .Returns(optionsRepository);

            var interview = new StatefulInterview(
                Create.Service.SubstitutionTextFactory(),
                Create.Service.InterviewTreeBuilder(),
                Create.Storage.QuestionnaireQuestionOptionsRepository()
                );

            interview.ServiceLocatorInstance = serviceLocator.Object;

            interview.CreateInterview(Create.Command.CreateInterview(
                interviewId: Guid.NewGuid(),
                userId: Guid.NewGuid(),
                questionnaireId: questionnaireId ?? new Guid("B000B000B000B000B000B000B000B000"),
                version: 1,
                answers: preloadedData.GetAnswers(),
                //answersTime: new DateTime(2012, 12, 20),
                supervisorId: Guid.NewGuid(),
                interviewerId: Guid.NewGuid(),
                interviewKey: Create.Entity.InterviewKey()));

            return interview;
        }

        public static StatefulInterview StatefulInterview(QuestionnaireDocument questionnaireDocument)
        {
            var questionnaireIdentity = new QuestionnaireIdentity(Guid.NewGuid(), 7);

            var questionnaireRepository = Create.Fake.QuestionnaireRepositoryWithOneQuestionnaire(
                questionnaireIdentity.QuestionnaireId,
                Create.Entity.PlainQuestionnaire(questionnaireDocument),
                questionnaireIdentity.Version
            );

            return StatefulInterview(
                questionnaireIdentity: questionnaireIdentity,
                questionnaireRepository: questionnaireRepository);
        }

        public static StatefulInterview StatefulInterview(QuestionnaireIdentity questionnaireIdentity,
            IQuestionnaireStorage questionnaireRepository = null, 
            List<InterviewAnswer> answersOnPrefilledQuestions = null,
            IQuestionOptionsRepository questionOptionsRepository = null)
        {
            var serviceLocatorMock = new Mock<IServiceLocator>();

            var qRepository = questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>();
            serviceLocatorMock.Setup(x => x.GetInstance<IQuestionnaireStorage>())
                .Returns(qRepository);

            var optionsRepository = questionOptionsRepository ?? Mock.Of<IQuestionOptionsRepository>();
            serviceLocatorMock.Setup(x => x.GetInstance<IQuestionOptionsRepository>())
                .Returns(optionsRepository);

            var interview = new StatefulInterview(
                Create.Service.SubstitutionTextFactory(),
                Create.Service.InterviewTreeBuilder(),
                optionsRepository);

            interview.ServiceLocatorInstance = serviceLocatorMock.Object;

            interview.CreateInterview(Create.Command.CreateInterview(
                interviewId: interview.EventSourceId,
                userId: new Guid("F111F111F111F111F111F111F111F111"),
                questionnaireId: questionnaireIdentity?.QuestionnaireId ?? new Guid("B000B000B000B000B000B000B000B000"),
                version: questionnaireIdentity?.Version ?? 1,
                answers: answersOnPrefilledQuestions ?? new List<InterviewAnswer>(),
                //answersTime: new DateTime(2012, 12, 20),
                supervisorId: Guid.NewGuid(),
                interviewerId: Guid.NewGuid(),
                interviewKey: Create.Entity.InterviewKey()));

          return interview;
        }

        public static CommittedEvent CommittedEvent(string origin = null, 
            Guid? eventSourceId = null,
            IEvent payload = null,
            Guid? eventIdentifier = null, 
            int eventSequence = 1)
        {
            return new CommittedEvent(
                Guid.Parse("33330000333330000003333300003333"),
                origin,
                eventIdentifier ?? Guid.Parse("44440000444440000004444400004444"),
                eventSourceId ?? Guid.Parse("55550000555550000005555500005555"),
                eventSequence,
                new DateTime(2014, 10, 22),
                0,
                payload ?? Mock.Of<IEvent>());
        }

        public static CommittedEventStream CommittedEventStream(Guid eventSourceId, IEnumerable<UncommittedEvent> events)
        {
            return new CommittedEventStream(eventSourceId,
                events
                    .Select(x => IntegrationCreate.CommittedEvent(payload: x.Payload,
                        eventSourceId: x.EventSourceId,
                        eventSequence: x.EventSequence)));
        }

        public static SequentialCommandService SequentialCommandService(
            IEventSourcedAggregateRootRepository repository = null, 
            ILiteEventBus eventBus = null)
        {
            var locatorMock = new Mock<IServiceLocator>();

            locatorMock.Setup(expression: x => x.GetInstance<IInScopeExecutor>())
                .Returns(valueFunction: () => new NoScopeInScopeExecutor(rootScope: locatorMock.Object));
            locatorMock.Setup(expression: x => x.GetInstance<ICommandExecutor>())
                .Returns(value: new CommandExecutor(eventSourcedRepository: repository ?? Mock.Of<IEventSourcedAggregateRootRepository>(),
                    eventBus: eventBus ?? Mock.Of<IEventBus>(),
                    serviceLocator: locatorMock.Object,
                    plainRepository: Mock.Of<IPlainAggregateRootRepository>(),
                    aggregateRootCache: Create.Storage.NewAggregateRootCache(),
                    commandsMonitoring: Mock.Of<ICommandsMonitoring>(),
                    prototypeService: Create.Service.MockOfAggregatePrototypeService(),
                    promoterService: Mock.Of<IAggregateRootPrototypePromoterService>()
                    ));

            return new SequentialCommandService(serviceLocator: locatorMock.Object, aggregateLock: Stub.Lock());
        }

        public static Answer Answer(string answer, decimal value, decimal? parentValue = null)
        {
            return new Answer()
            {
                AnswerText = answer,
                AnswerValue = value.ToString(),
                ParentValue = parentValue.HasValue ? parentValue.ToString() : null
            };
        }

        public static FixedRosterTitle FixedTitle(decimal value, string title = null)
        {
            return new FixedRosterTitle(value, title ?? ("Roster " + value));
        }

        public static LookupTableContent LookupTableContent(string[] variableNames, params LookupTableRow[] rows)
        {
            return new LookupTableContent
            (
                variableNames : variableNames,
                rows : rows
            );
        }

        public static LookupTableRow LookupTableRow(long rowcode, decimal?[] values)
        {
            return new LookupTableRow
                   {
                       RowCode = rowcode,
                       Variables = values
            };
        }

        public static PostgresKeyValueStorage<TEntity> PostgresReadSideKeyValueStorage<TEntity>(
            IUnitOfWork sessionProvider = null, UnitOfWorkConnectionSettings postgreConnectionSettings = null)
            where TEntity : class, IReadSideRepositoryEntity
        {
            return new PostgresKeyValueStorage<TEntity>(
                sessionProvider ?? Mock.Of<IUnitOfWork>(),
                new EntitySerializer<TEntity>());
        }

        public static ISessionFactory SessionFactory(string connectionString, 
            IEnumerable<Type> painStorageEntityMapTypes,
            bool executeSchemaUpdate, string schemaName = null)
        {
            var cfg = new Configuration();
            
            cfg.DataBaseIntegration(db =>
            {
                db.ConnectionString = connectionString;
                var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    SearchPath = schemaName
                };

                var workspaceConnectionString = connectionStringBuilder.ToString();
                db.ConnectionString = workspaceConnectionString;
                db.Dialect<PostgreSQL91Dialect>();
                db.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
            });

            cfg.AddDeserializedMapping(GetMappingsFor(painStorageEntityMapTypes, schemaName), "Plain");
            cfg.SetProperty(NHibernate.Cfg.Environment.WrapResultSets, "true");

            if (schemaName != null)
            {
                cfg.SetProperty(NHibernate.Cfg.Environment.DefaultSchema, schemaName);
            }
            
            if (executeSchemaUpdate)
            {
                var update = new SchemaUpdate(cfg);
                update.Execute(false, true);
            }
           
            return cfg.BuildSessionFactory();
        }
        public static ISessionFactory SessionFactoryProd(string connectionString, Workspace workspace = null)
        {
            workspace ??= Workspace.Default;
            
            return new HqSessionFactoryFactory(Startup.BuildUnitOfWorkSettings(connectionString))
                .BuildSessionFactory(workspace.AsContext().SchemaName);
        }

        public static ISessionFactory SessionFactory(string connectionString, 
            string usersSchemaName,
            IEnumerable<Type> usersEntityMapTypes,
            string readSideSchemaName,
            IEnumerable<Type> readStorageEntityMapTypes,
            bool executeSchemaUpdate)
        {
            var cfg = new Configuration();
            
            cfg.DataBaseIntegration(db =>
            {
                db.ConnectionString = connectionString;
                var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    SearchPath = readSideSchemaName
                };

                var workspaceConnectionString = connectionStringBuilder.ToString();
                db.ConnectionString = workspaceConnectionString;
                db.Dialect<PostgreSQL91Dialect>();
                db.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
            });

            cfg.AddDeserializedMapping(GetUsersMappings(usersEntityMapTypes), "user");
            cfg.AddDeserializedMapping(GetMappingsFor(readStorageEntityMapTypes, readSideSchemaName), "read");
            cfg.SetProperty(NHibernate.Cfg.Environment.WrapResultSets, "true");

            if (executeSchemaUpdate)
            {
                var update = new SchemaUpdate(cfg);
                update.Execute(false, true);
            }
            
            return cfg.BuildSessionFactory();
        }

        private static HbmMapping GetUsersMappings(IEnumerable<Type> assemblies)
        {
            var mapper = new ModelMapper();
            var readSideMappingTypes = assemblies
                .Where(x => x.GetCustomAttribute<UsersAttribute>() != null &&
                            x.IsSubclassOfRawGeneric(typeof(ClassMapping<>)));

            mapper.AfterMapProperty += (inspector, member, customizer) =>
            {
                var propertyInfo = (PropertyInfo)member.LocalMember;

                customizer.Column('"' + propertyInfo.Name + '"');
            };

            mapper.AddMappings(readSideMappingTypes);

            return mapper.CompileMappingForAllExplicitlyAddedEntities();
        }

        public static IUnitOfWork UnitOfWork(ISessionFactory factory,
            IWorkspaceContextAccessor workspaceContextAccessor = null)
        {
            return new UnitOfWork(new Lazy<ISessionFactory>(() => factory), 
                Mock.Of<ILogger<UnitOfWork>>(),  workspaceContextAccessor ?? Create.Service.WorkspaceContextAccessor(), Mock.Of<ILifetimeScope>());
        }

        private static HbmMapping GetMappingsFor(IEnumerable<Type> painStorageEntityMapTypes, string schemaName = null)
        {
            var mapper = new ModelMapper();
            mapper.AddMappings(painStorageEntityMapTypes);
            mapper.BeforeMapProperty += (inspector, member, customizer) =>
            {
                var propertyInfo = (PropertyInfo)member.LocalMember;
                if (propertyInfo.PropertyType == typeof(string))
                {
                    customizer.Type(NHibernateUtil.StringClob);
                }
            };
            mapper.BeforeMapClass += (inspector, type, customizer) =>
            {
                var tableName = type.Name.Pluralize();
                customizer.Table(tableName);
                
                if (schemaName != null)
                    customizer.Schema(schemaName);
            };

            if (schemaName != null)
            {
                mapper.BeforeMapSet += (inspector, member, customizer) => customizer.Schema(schemaName);
                mapper.BeforeMapBag += (inspector, member, customizer) => customizer.Schema(schemaName);
                mapper.BeforeMapList += (inspector, member, customizer) => customizer.Schema(schemaName);
            }

            return mapper.CompileMappingForAllExplicitlyAddedEntities();
        }

        public static CumulativeReportStatusChange CumulativeReportStatusChange(Guid? questionnaireId=null, long? questionnaireVersion=null, DateTime? date = null, Guid? interviewId = null, long eventSequence = 1)
        {
            return new CumulativeReportStatusChange(Guid.NewGuid().FormatGuid(), questionnaireId ?? Guid.NewGuid(),
                questionnaireVersion ?? 1, date??DateTime.Now, InterviewStatus.Completed, 1, interviewId ?? Guid.NewGuid(), eventSequence);
        }

        public static DesignerEngineVersionService DesignerEngineVersionService()
            => new DesignerEngineVersionService(Mock.Of<IAttachmentService>(), Mock.Of<IDesignerTranslationService>());

        public static PostgreReadSideStorage<TEntity> PostgresReadSideRepository<TEntity>(
            IUnitOfWork sessionProvider = null)
            where TEntity : class, IReadSideRepositoryEntity
        {
            return new PostgreReadSideStorage<TEntity>(sessionProvider ?? Mock.Of<IUnitOfWork>(), Create.Storage.NewMemoryCache());
        }  
        
        public static PostgreReadSideStorage<TEntity, TK> PostgresReadSideRepository<TEntity, TK>(
            IUnitOfWork sessionProvider = null)
            where TEntity : class, IReadSideRepositoryEntity
        {
            return new PostgreReadSideStorage<TEntity, TK>(sessionProvider ?? Mock.Of<IUnitOfWork>(), Create.Storage.NewMemoryCache());
        }

        public static AnswerNotifier AnswerNotifier(IViewModelEventRegistry registry = null)
            => new AnswerNotifier(registry ?? Abc.Create.Service.LiteEventRegistry());

        public static IDictionary<Identity, IReadOnlyList<FailedValidationCondition>> FailedValidationCondition(Identity questionIdentity)
            => new Dictionary<Identity, IReadOnlyList<FailedValidationCondition>>
            {
                {
                    questionIdentity,
                    new List<FailedValidationCondition>() {new FailedValidationCondition(0)}
                }
            };

        public static PreloadedDataDto PreloadedDataDto(params PreloadedLevelDto[] levels)
        {
            return new PreloadedDataDto(levels);
        }

        public static PreloadedLevelDto PreloadedLevelDto(RosterVector rosterVector, Dictionary<Guid, AbstractAnswer> answers)
        {
            return new PreloadedLevelDto(rosterVector, answers);
        }
    }
}
