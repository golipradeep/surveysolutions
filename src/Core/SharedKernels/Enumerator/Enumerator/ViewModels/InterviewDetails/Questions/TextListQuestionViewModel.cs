﻿using System;
using System.Linq;
using System.Threading.Tasks;
using MvvmCross;
using MvvmCross.Base;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Utils;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;
using WB.Core.SharedKernels.SurveySolutions.Documents;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions
{
    public class TextListQuestionViewModel : MvxNotifyPropertyChanged, 
        IInterviewEntityViewModel, 
        IDisposable, 
        ICompositeQuestionWithChildren
    {
        private readonly IPrincipal principal;
        private readonly IQuestionnaireStorage questionnaireRepository;
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IUserInteractionService userInteractionService;
        private readonly IMvxMainThreadAsyncDispatcher mainThreadDispatcher;
        private string interviewId;
        private bool isRosterSizeQuestion;
        private int? maxAnswerCount;

        public QuestionInstructionViewModel InstructionViewModel { get; private set; }
        private readonly QuestionStateViewModel<TextListQuestionAnswered> questionState;

        private TextListAddNewItemViewModel addNewItemViewModel
            => this.Answers?.OfType<TextListAddNewItemViewModel>().FirstOrDefault();

        public IQuestionStateViewModel QuestionState => this.questionState;
        public AnsweringViewModel Answering { get; private set; }

        public CovariantObservableCollection<ICompositeEntity> Answers { get;  }
        
        public TextListQuestionViewModel(
            IPrincipal principal,
            IQuestionnaireStorage questionnaireRepository,
            IStatefulInterviewRepository interviewRepository,
            QuestionStateViewModel<TextListQuestionAnswered> questionStateViewModel,
            IUserInteractionService userInteractionService,
            AnsweringViewModel answering,
            QuestionInstructionViewModel instructionViewModel)
        {
            this.principal = principal;
            this.questionnaireRepository = questionnaireRepository;
            this.interviewRepository = interviewRepository;
            this.questionState = questionStateViewModel;
            this.InstructionViewModel = instructionViewModel;
            this.userInteractionService = userInteractionService;
            this.mainThreadDispatcher = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();
            this.Answering = answering;
            this.Answers = new CovariantObservableCollection<ICompositeEntity>();
        }
        public Identity Identity { get; private set; }

        public void Init(string interviewId, Identity entityIdentity, NavigationState navigationState)
        {
            this.Identity = entityIdentity ?? throw new ArgumentNullException(nameof(entityIdentity));
            this.interviewId = interviewId ?? throw new ArgumentNullException(nameof(interviewId));

            this.InstructionViewModel.Init(interviewId, entityIdentity, navigationState);
            this.questionState.Init(interviewId, entityIdentity, navigationState);

            var interview = this.interviewRepository.Get(interviewId);
            
            var questionnaire = this.questionnaireRepository.GetQuestionnaire(interview.QuestionnaireIdentity, interview.Language);
            this.isRosterSizeQuestion = questionnaire.IsRosterSizeQuestion(this.Identity.Id);
            this.maxAnswerCount = questionnaire.GetMaxSelectedAnswerOptions(this.Identity.Id);

            var textListQuestion = interview.GetTextListQuestion(entityIdentity);
            if (textListQuestion.IsAnswered())
            {
                var answerViewModels = textListQuestion.GetAnswer().ToTupleArray().Select(x => this.CreateListItemViewModel(x.Item1, x.Item2, interview));

                answerViewModels.ForEach(answerViewModel => this.Answers.Add(answerViewModel));
            }
            
            this.ShowOrHideAddNewItem();
        }

        private void ShowOrHideAddNewItem()
        {
            var answerViewModels = this.Answers.OfType<TextListItemViewModel>().ToList();

            bool denyAddNewItem = (this.maxAnswerCount.HasValue && answerViewModels.Count >= maxAnswerCount.Value) ||
                                  (this.isRosterSizeQuestion && answerViewModels.Count >= Constants.MaxLongRosterRowCount);

            if (denyAddNewItem && this.addNewItemViewModel != null)
            {
                this.Answers.Remove(addNewItemViewModel);
            }
            else if (!denyAddNewItem && this.addNewItemViewModel == null)
            {
                var viewModel = new TextListAddNewItemViewModel(this.questionState, this.AddListItem);
                this.Answers.Add(viewModel);
            }
        }

        private async void ListItemDeleted(object sender, EventArgs eventArgs)
        {
            var listItem = sender as TextListItemViewModel;

            if (listItem == null)
                return;

            if (this.isRosterSizeQuestion )
            {
                var message = UIResources.Interview_Questions_RemoveRowFromRosterMessage.FormatString(listItem.Title);
                if (!(await this.userInteractionService.ConfirmAsync(message)))
                {
                    return;
                }
            }

            listItem.ItemEdited -= this.ListItemEdited;
            listItem.ItemDeleted -= this.ListItemDeleted;

            this.InvokeOnMainThread(() => this.Answers.Remove(listItem));

            await this.SaveAnswers();
        }

        private IMvxAsyncCommand<string> AddListItem => new MvxAsyncCommand<string>(AddListItemCmd);

        private async Task AddListItemCmd(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg)) return;
            var answer = arg.Trim();

            var answerViewModels = this.Answers.OfType<TextListItemViewModel>().ToList();
            var maxValue = answerViewModels.Count == 0
                ? 1
                : answerViewModels.Max(x => x.Value) + 1;

            var interview = this.interviewRepository.Get(this.interviewId);
            var itemViewModel = this.CreateListItemViewModel(maxValue, answer, interview);
            var insertPosition = this.Answers.Count - 1;
            await mainThreadDispatcher.ExecuteOnMainThreadAsync(() => this.Answers.Insert(insertPosition, itemViewModel));

            await this.SaveAnswers().ConfigureAwait(false);

            if (this.addNewItemViewModel != null)
                this.addNewItemViewModel.Text = string.Empty;
        }

        private async void ListItemEdited(object sender, EventArgs eventArgs) => await this.SaveAnswers();

        private async Task SaveAnswers()
        {
            if (!this.principal.IsAuthenticated) return;

            var answerViewModels = this.Answers.OfType<TextListItemViewModel>().ToList();

            if (answerViewModels.Any(x => string.IsNullOrWhiteSpace(x.Title)))
            {
                await this.questionState.Validity.MarkAnswerAsNotSavedWithMessage(UIResources.Interview_Question_List_Empty_Values_Are_Not_Allowed);
                return;
            }

            var answers = answerViewModels.Select(x => new Tuple<decimal, string>(x.Value, x.Title)).ToArray();

            var command = new AnswerTextListQuestionCommand(
                interviewId: Guid.Parse(this.interviewId),
                userId: this.principal.CurrentUserIdentity.UserId,
                questionId: this.Identity.Id,
                rosterVector: this.Identity.RosterVector,
                answers: answers);

            try
            {
                await this.Answering.SendQuestionCommandAsync(command).ConfigureAwait(false);
                await this.questionState.Validity.ExecutedWithoutExceptions();
                await this.mainThreadDispatcher.ExecuteOnMainThreadAsync(this.ShowOrHideAddNewItem).ConfigureAwait(false);
            }
            catch (InterviewException ex)
            {
                await this.questionState.Validity.ProcessException(ex);
            }
        }

        private TextListItemViewModel CreateListItemViewModel(decimal value, string title, IStatefulInterview interview)
        {
            var optionViewModel = new TextListItemViewModel(this.questionState)
            {
                Value = value,
                Title = title
            };

            optionViewModel.ItemEdited += this.ListItemEdited;
            optionViewModel.ItemDeleted += this.ListItemDeleted;
            optionViewModel.IsProtected = interview.IsAnswerProtected(this.Identity, value);

            return optionViewModel;
        }

        public void Dispose()
        {
            Answers.OfType<TextListItemViewModel>().ForEach(x =>
            {
                x.ItemDeleted -= this.ListItemDeleted;
                x.ItemEdited -= ListItemEdited;
            });
            
            this.questionState.Dispose();
            this.InstructionViewModel.Dispose();
        }

        public IObservableCollection<ICompositeEntity> Children
        {
            get
            {
                var result = new CompositeCollection<ICompositeEntity>();                    
                result.Add(new OptionBorderViewModel(this.questionState, true));
                result.AddCollection(this.Answers);
                result.Add(new OptionBorderViewModel(this.questionState, false));
                return result;
            }
        }
    }
}
