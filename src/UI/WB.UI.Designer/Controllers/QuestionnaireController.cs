﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using WB.Core.BoundedContexts.Designer;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire;
using WB.Core.BoundedContexts.Designer.DataAccess;
using WB.Core.BoundedContexts.Designer.MembershipProvider;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Verifier;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.ChangeHistory;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.QuestionnaireInfo;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.Questionnaire.Translations;
using WB.UI.Designer.Code;
using WB.UI.Designer.Code.ImportExport;
using WB.UI.Designer.Extensions;
using WB.UI.Designer.Filters;
using WB.UI.Designer.Resources;

namespace WB.UI.Designer.Controllers
{
    [Authorize]
    [ResponseCache(NoStore = true)]
    public partial class QuestionnaireController : Controller
    {
        public class QuestionnaireCloneModel
        {
            [Key]
            public Guid QuestionnaireId { get; set; }

            public Guid? Revision { get; set; }

            [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.QuestionnaireTitle_required))]
            [StringLength(AbstractVerifier.MaxTitleLength, ErrorMessageResourceName = nameof(ErrorMessages.QuestionnaireTitle_MaxLength), ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessage = null)]
            public string? Title { get; set; }
        }

        public class QuestionnaireViewModel
        {
            [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = "QuestionnaireTitle_required")]
            [StringLength(AbstractVerifier.MaxTitleLength, ErrorMessageResourceName = nameof(ErrorMessages.QuestionnaireTitle_MaxLength), ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessage = null)]
            public string? Title { get; set; }

            [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = "QuestionnaireVariable_required")]
            [RegularExpression(AbstractVerifier.VariableRegularExpression, ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.QuestionnaireVariable_rules))]
            [StringLength(AbstractVerifier.DefaultVariableLengthLimit, ErrorMessageResourceName = nameof(ErrorMessages.QuestionnaireVariable_MaxLength), ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessage = null)]
            public string? Variable { get; set; }

            public bool IsPublic { get; set; }
        }

        private class ComboItem
        {
            public string? Name { get; set; }
            public Guid? Value { get; set; }
        }

        private readonly ICommandService commandService;
        private readonly IQuestionnaireChangeHistoryFactory questionnaireChangeHistoryFactory;
        private readonly IQuestionnaireViewFactory questionnaireViewFactory;
        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly ILookupTableService lookupTableService;
        private readonly IQuestionnaireInfoFactory questionnaireInfoFactory;
        private readonly ILogger<QuestionnaireController> logger;
        private readonly IQuestionnaireInfoViewFactory questionnaireInfoViewFactory;
        private readonly ICategoricalOptionsImportService categoricalOptionsImportService;
        private readonly DesignerDbContext dbContext;
        private readonly IQuestionnaireHelper questionnaireHelper;
        private readonly IPublicFoldersStorage publicFoldersStorage;
        private readonly ICategoriesService categoriesService;
        private readonly IQuestionnaireHistoryVersionsService questionnaireHistoryVersionsService;

        public QuestionnaireController(
            IQuestionnaireViewFactory questionnaireViewFactory,
            IFileSystemAccessor fileSystemAccessor,
            ILogger<QuestionnaireController> logger,
            IQuestionnaireInfoFactory questionnaireInfoFactory,
            IQuestionnaireChangeHistoryFactory questionnaireChangeHistoryFactory,
            IQuestionnaireHistoryVersionsService questionnaireHistoryVersionsService,
            ILookupTableService lookupTableService,
            IQuestionnaireInfoViewFactory questionnaireInfoViewFactory,
            ICategoricalOptionsImportService categoricalOptionsImportService,
            ICommandService commandService,
            DesignerDbContext dbContext,
            IQuestionnaireHelper questionnaireHelper,
            IPublicFoldersStorage publicFoldersStorage,
            ICategoriesService categoriesService)
        {
            this.questionnaireViewFactory = questionnaireViewFactory;
            this.fileSystemAccessor = fileSystemAccessor;
            this.logger = logger;
            this.questionnaireInfoFactory = questionnaireInfoFactory;
            this.questionnaireChangeHistoryFactory = questionnaireChangeHistoryFactory;
            this.lookupTableService = lookupTableService;
            this.questionnaireInfoViewFactory = questionnaireInfoViewFactory;
            this.categoricalOptionsImportService = categoricalOptionsImportService;
            this.commandService = commandService;
            this.dbContext = dbContext;
            this.questionnaireHelper = questionnaireHelper;
            this.publicFoldersStorage = publicFoldersStorage;
            this.categoriesService = categoriesService;
            this.questionnaireHistoryVersionsService = questionnaireHistoryVersionsService;
        }

        [Route("questionnaire/details/{id}/nosection/{entityType}/{entityId}")]
        public IActionResult DetailsNoSection(QuestionnaireRevision id,
            Guid? chapterId, string entityType, Guid? entityid)
        {
            if (User.IsAdmin() || this.UserHasAccessToEditOrViewQuestionnaire(id.QuestionnaireId))
            {
                // get section id and redirect
                var sectionId = questionnaireInfoFactory.GetSectionIdForItem(id, entityid);
                return RedirectToActionPermanent("Details", new RouteValueDictionary
                {
                    { "id", id.ToString() }, {"chapterId", sectionId.FormatGuid()},{ "entityType", entityType},{ "entityid", entityid.FormatGuid()}
                });
            }

            return this.LackOfPermits();
        }

        [AntiForgeryFilter]
        [Route("questionnaire/details/{id}")]
        [Route("questionnaire/details/{id}/chapter/{chapterId}/{entityType}/{entityId}")]
        public IActionResult Details(QuestionnaireRevision? id, Guid? chapterId, string entityType, Guid? entityid)
        {
            if(id == null)
                return this.RedirectToAction("Index");

            return (User.IsAdmin() || this.UserHasAccessToEditOrViewQuestionnaire(id.QuestionnaireId))
                ? this.View("~/questionnaire/index.cshtml")
                : this.LackOfPermits();
        }

        private bool UserHasAccessToEditOrViewQuestionnaire(Guid id)
        {
            return this.questionnaireViewFactory.HasUserAccessToQuestionnaire(id, User.GetId());
        }

        public IActionResult Clone(QuestionnaireRevision id)
        {
            QuestionnaireView? questionnaire = this.GetQuestionnaireView(id.QuestionnaireId);
            if (questionnaire == null) return NotFound();

            QuestionnaireView model = questionnaire;
            return View(
                    new QuestionnaireCloneModel
                    {
                        Title = $"Copy of {model.Title}",
                        QuestionnaireId = id.QuestionnaireId,
                        Revision = id.Revision
                    });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clone(QuestionnaireCloneModel model)
        {
            if (this.ModelState.IsValid)
            {
                QuestionnaireView? questionnaire =
                    this.questionnaireViewFactory.Load(new QuestionnaireRevision(model.QuestionnaireId, model.Revision));

                if (questionnaire == null)
                    return NotFound();

                QuestionnaireView sourceModel = questionnaire;
                try
                {
                    var questionnaireId = Guid.NewGuid();

                    var command = new CloneQuestionnaire(questionnaireId, model.Title ?? "", User.GetId(),
                        false, sourceModel.Source);

                    this.commandService.Execute(command);

                    await dbContext.SaveChangesAsync();

                    return this.RedirectToAction("Details", "Questionnaire", new { id = questionnaireId.FormatGuid() });
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error on questionnaire cloning.");

                    var domainException = e.GetSelfOrInnerAs<QuestionnaireException>();
                    if (domainException != null)
                    {
                        this.Error(domainException.Message);
                        logger.LogError(domainException, "Questionnaire controller -> clone: " + domainException.Message);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return this.View(model);
        }

        public IActionResult Create()
        {
            return this.View(new QuestionnaireViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(QuestionnaireViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                var questionnaireId = Guid.NewGuid();

                try
                {
                    var command = new CreateQuestionnaire(
                        questionnaireId: questionnaireId,
                        text: model.Title ?? "",
                        responsibleId: User.GetId(),
                        isPublic: model.IsPublic,
                        variable: model.Variable ?? "");

                    this.commandService.Execute(command);
                    this.dbContext.SaveChanges();

                    return this.RedirectToAction("Details", "Questionnaire", new { id = questionnaireId.FormatGuid() });
                }
                catch (QuestionnaireException e)
                {
                    this.Error(e.Message);
                    logger.LogError(e, "Error on questionnaire creation.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            QuestionnaireView? model = this.GetQuestionnaireView(id);
            if (model != null)
            {
                if ((model.CreatedBy != User.GetId()) && !User.IsAdmin())
                {
                    this.Error(Resources.QuestionnaireController.ForbiddenDelete);
                }
                else
                {
                    var command = new DeleteQuestionnaire(model.PublicKey, User.GetId());
                    this.commandService.Execute(command);

                    this.Success(string.Format(Resources.QuestionnaireController.SuccessDeleteMessage, model.Title));
                }
            }
            return this.RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Revert(Guid id, Guid commandId)
        {
            var historyReferenceId = commandId;

            bool hasAccess = this.User.IsAdmin() || this.questionnaireViewFactory.HasUserAccessToRevertQuestionnaire(id, this.User.GetId());
            if (!hasAccess)
            {
                this.Error(Resources.QuestionnaireController.ForbiddenRevert);
                return this.RedirectToAction("Index");
            }

            var command = new RevertVersionQuestionnaire(id, historyReferenceId, this.User.GetId());
            this.commandService.Execute(command);

            string sid = id.FormatGuid();
            return this.RedirectToAction("Details", new { id = sid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<bool>> SaveComment(Guid id, Guid historyItemId, string comment)
        {
            bool hasAccess = this.User.IsAdmin() 
                || this.questionnaireViewFactory.HasUserAccessToRevertQuestionnaire(id, this.User.GetId());

            if (!hasAccess)
                return false;

            var canEdit = this.questionnaireViewFactory.HasUserAccessToEditComments(historyItemId, this.User.GetId());

            if (!canEdit) return false;

            return await this.questionnaireHistoryVersionsService.UpdateRevisionCommentaryAsync(
                historyItemId.FormatGuid(), comment);
        }

        [AntiForgeryFilter]
        public async Task<IActionResult> QuestionnaireHistory(Guid id, int? p)
        {
            bool hasAccess = this.User.IsAdmin() || this.questionnaireViewFactory.HasUserAccessToQuestionnaire(id, this.User.GetId());
            if (!hasAccess)
            {
                this.Error(ErrorMessages.NoAccessToQuestionnaire);
                return this.RedirectToAction("Index");
            }
            var questionnaireInfoView = this.questionnaireInfoViewFactory.Load(new QuestionnaireRevision(id), this.User.GetId());
            if (questionnaireInfoView == null) return NotFound();

            QuestionnaireChangeHistory? questionnairePublicListViewModels = 
                await questionnaireChangeHistoryFactory.LoadAsync(id, p ?? 1, GlobalHelper.GridPageItemsCount, this.User);
            if (questionnairePublicListViewModels == null)
                return NotFound();

            questionnairePublicListViewModels.ReadonlyMode = questionnaireInfoView.IsReadOnlyForUser;

            return this.View(questionnairePublicListViewModels);
        }

        public IActionResult ExpressionGeneration(Guid? id)
        {
            ViewBag.QuestionnaireId = id ?? Guid.Empty;
            return this.View();
        }

        private QuestionnaireView? GetQuestionnaireView(Guid id)
        {
            QuestionnaireView? questionnaire = this.questionnaireViewFactory.Load(new QuestionnaireViewInputModel(id));
            return questionnaire;
        }

        public IActionResult LackOfPermits()
        {
            this.Error(Resources.QuestionnaireController.Forbidden);
            return this.RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GetLanguages(QuestionnaireRevision id)
        {
            QuestionnaireView? questionnaire = this.questionnaireViewFactory.Load(id);
            if (questionnaire == null) return NotFound();

            var comboBoxItems =
                new ComboItem
                {
                    Name = !string.IsNullOrEmpty(questionnaire.Source.DefaultLanguageName) ?
                        questionnaire.Source.DefaultLanguageName :
                        QuestionnaireHistoryResources.Translation_Original,
                    Value = null
                }.ToEnumerable()
                .Concat(
                    questionnaire.Source.Translations.Select(
                        i => new ComboItem { Name = i.Name ?? Resources.QuestionnaireController.Untitled, Value = i.Id })
                );
            return this.Json(comboBoxItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignFolder(Guid id, Guid folderId)
        {
            QuestionnaireView? questionnaire = GetQuestionnaireView(id);
            if (questionnaire == null)
                return NotFound();

            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            {
                return this.Redirect(referer);
            }
            
            return Redirect(Url.Content("~/"));
        }

        /*[HttpGet]
        [Authorize(Roles = "Administrator")]
        public FileResult? Backup(Guid id)
        {
            var stream = this.questionnaireBackupService.GetBackupQuestionnaire(id, out string questionnaireFileName);
            
            return stream == null
                    ? null
                    : File(stream, "application/zip", $"{questionnaireFileName}.zip");
            
        }*/
    }
}
