using System;
using System.Linq;
using FluentAssertions;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Tests.Abc;

namespace WB.Tests.Integration.InterviewTests.LinkedQuestions
{
    internal class when_answering_linked_single_option_question_which_links_to_datetime_question_and_which_is_roster_title_for_2_rosters_and_roster_level_is_1 : InterviewTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFF1111111111");
            var questionnaireId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDD0000000000");

            var rosterInstanceId = 0;
            rosterVector = Create.RosterVector(rosterInstanceId);

            questionId = Guid.Parse("11111111111111111111111111111111");
            var linkedToQuestionId = Guid.Parse("33333333333333333333333333333333");
            var linkedToRosterId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
            rosterAId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            rosterBId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");

            var linkedOption1Vector = Create.RosterVector(0);
            linkedOption2Vector = Create.RosterVector(1);
            var linkedOption3Vector = Create.RosterVector(2);
            var linkedOption1Answer = new DateTime(2014, 2, 23);
            var linkedOption2Answer = new DateTime(2014, 3, 8);
            var linkedOption3Answer = new DateTime(2014, 5, 9);
            linkedOption2TextInvariantCulture = linkedOption2Answer.ToString(DateTimeFormat.DateFormat);//"03/08/2014";
            
            var triggerQuestionId = Guid.NewGuid();
            var questionnaireDocument = Abc.Create.Entity.QuestionnaireDocumentWithOneChapter(id: questionnaireId, children: new IComposite[]
            {
                Create.Entity.NumericIntegerQuestion(id: triggerQuestionId, variable: "num_trigger"),
                Abc.Create.Entity.Roster(rosterId: rosterAId, rosterSizeSourceType: RosterSizeSourceType.Question,
                    rosterSizeQuestionId: triggerQuestionId, rosterTitleQuestionId: questionId, variable: "ros1",
                    children: new IComposite[]
                    {
                        Abc.Create.Entity.SingleQuestion(id: questionId, linkedToQuestionId: linkedToQuestionId,
                            variable: "link_single")
                    }),
                Abc.Create.Entity.Roster(rosterId: rosterBId, rosterSizeSourceType: RosterSizeSourceType.Question,
                    rosterSizeQuestionId: triggerQuestionId, variable: "ros2", rosterTitleQuestionId: questionId),
                Abc.Create.Entity.Roster(rosterId: linkedToRosterId, fixedRosterTitles: new [] { IntegrationCreate.FixedTitle(0), IntegrationCreate.FixedTitle(1), IntegrationCreate.FixedTitle(2)}, variable: "ros3",
                    children: new IComposite[]
                    {
                        Abc.Create.Entity.DateTimeQuestion(questionId: linkedToQuestionId, variable: "link_source")
                    })
            });
            appDomainContext = AppDomainContext.Create();

            interview = SetupInterview(appDomainContext.AssemblyLoadContext, questionnaireDocument: questionnaireDocument);

            interview.AnswerDateTimeQuestion(userId, linkedToQuestionId, linkedOption1Vector, DateTime.Now, linkedOption1Answer);
            interview.AnswerDateTimeQuestion(userId, linkedToQuestionId, linkedOption2Vector, DateTime.Now, linkedOption2Answer);
            interview.AnswerDateTimeQuestion(userId, linkedToQuestionId, linkedOption3Vector, DateTime.Now, linkedOption3Answer);
            interview.AnswerNumericIntegerQuestion(userId, triggerQuestionId, RosterVector.Empty, DateTime.Now, 1);

            interview.Apply(new LinkedOptionsChanged(new[]
            {
                new ChangedLinkedOptions(Create.Identity(questionId, rosterVector), new RosterVector[]
                {
                    linkedOption1Vector, linkedOption2Vector, linkedOption3Vector
                })
            }, DateTimeOffset.Now));

            eventContext = new EventContext();
            BecauseOf();
        }

        public void BecauseOf() =>
            interview.AnswerSingleOptionLinkedQuestion(userId, questionId, rosterVector, DateTime.Now, linkedOption2Vector);

        [NUnit.Framework.OneTimeTearDown] public void CleanUp()
        {
            eventContext.Dispose();
            eventContext = null;
            appDomainContext.Dispose();
        }

        [NUnit.Framework.Test] public void should_raise_SingleOptionLinkedQuestionAnswered_event () =>
            eventContext.ShouldContainEvent<SingleOptionLinkedQuestionAnswered>();

        [NUnit.Framework.Test] public void should_raise_1_RosterRowsTitleChanged_events () =>
            eventContext.ShouldContainEvents<RosterInstancesTitleChanged>(count: 1);

        [NUnit.Framework.Test] public void should_set_2_affected_roster_ids_in_RosterRowsTitleChanged_events () =>
            eventContext.GetEvents<RosterInstancesTitleChanged>().SelectMany(@event => @event.ChangedInstances.Select(r => r.RosterInstance.GroupId)).ToArray()
                .Should().BeEquivalentTo(rosterAId, rosterBId);

        [NUnit.Framework.Test] public void should_set_empty_outer_roster_vector_to_all_RosterRowTitleChanged_events () =>
                eventContext.GetEvents<RosterInstancesTitleChanged>()
                .Should().OnlyContain(@event => @event.ChangedInstances.All(x => x.RosterInstance.OuterRosterVector.Length == 0));

        [NUnit.Framework.Test] public void should_set_last_element_of_roster_vector_to_roster_instance_id_in_all_RosterRowTitleChanged_events () =>
            eventContext.GetEvents<RosterInstancesTitleChanged>()
                .Should().OnlyContain(@event => @event.ChangedInstances.All(x => x.RosterInstance.RosterInstanceId == rosterVector.Last()));

        [NUnit.Framework.Test] public void should_set_title_to_invariant_culture_formatted_value_assigned_to_corresponding_linked_to_question_in_all_RosterRowTitleChanged_events () =>
            eventContext.GetEvents<RosterInstancesTitleChanged>()
                .SelectMany(@event => @event.ChangedInstances.Select(x => x.Title))
                .Should().OnlyContain(title => title == linkedOption2TextInvariantCulture);

        private static AppDomainContext appDomainContext;
        private static EventContext eventContext;
        private static Interview interview;
        private static Guid userId;
        private static Guid questionId;
        private static int[] rosterVector;
        private static Guid rosterAId;
        private static Guid rosterBId;
        private static RosterVector linkedOption2Vector;
        private static string linkedOption2TextInvariantCulture;
    }
}
