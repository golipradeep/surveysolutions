﻿using System;
using System.IO;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Utils;
using WB.Core.SharedKernels.Enumerator.Implementation.Services;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Utils;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;
using Identity = WB.Core.SharedKernels.DataCollection.Identity;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions
{
    public class MultimediaQuestionViewModel : MvxNotifyPropertyChanged,
        IInterviewEntityViewModel,
        IViewModelEventHandler<AnswersRemoved>,
        ICompositeQuestion,
        IDisposable
    {
        private readonly Guid userId;
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly IPictureChooser pictureChooser;
        private readonly IUserInteractionService userInteractionService;
        private readonly IInterviewFileStorage imageFileStorage;
        private readonly IViewModelEventRegistry eventRegistry;
        private readonly IViewModelNavigationService viewModelNavigationService;
        private Guid interviewId;
        private Identity questionIdentity;
        private string variableName;
        private byte[] answer;

        public MultimediaQuestionViewModel(
            IPrincipal principal,
            IStatefulInterviewRepository interviewRepository,
            IImageFileStorage imageFileStorage,
            IViewModelEventRegistry eventRegistry,
            IQuestionnaireStorage questionnaireStorage,
            IPictureChooser pictureChooser,
            IUserInteractionService userInteractionService,
            IViewModelNavigationService viewModelNavigationService,
            QuestionStateViewModel<PictureQuestionAnswered> questionStateViewModel,
            QuestionInstructionViewModel instructionViewModel,
            AnsweringViewModel answering)
        {
            this.userId = principal.CurrentUserIdentity.UserId;
            this.interviewRepository = interviewRepository;
            this.imageFileStorage = imageFileStorage;
            this.eventRegistry = eventRegistry;
            this.questionnaireStorage = questionnaireStorage;
            this.pictureChooser = pictureChooser;
            this.userInteractionService = userInteractionService;
            this.questionState = questionStateViewModel;
            this.InstructionViewModel = instructionViewModel;
            this.Answering = answering;
            this.viewModelNavigationService = viewModelNavigationService;
        }

        public AnsweringViewModel Answering { get; }

        public byte[] Answer
        {
            get => this.answer;
            set
            {
                this.answer = value;
                this.RaisePropertyChanged();
            }
        }

        public Identity Identity => this.questionIdentity;
        public IMvxAsyncCommand RequestAnswerCommand => new MvxAsyncCommand(this.RequestAnswerAsync);
        public IMvxAsyncCommand RemoveAnswerCommand => new MvxAsyncCommand(this.RemoveAnswerAsync);

        public IMvxAsyncCommand ShowPhotoView => new MvxAsyncCommand(async ()=> 
        {
            if (this.Answer?.Length > 0)
            {
                await this.viewModelNavigationService.NavigateToAsync<PhotoViewViewModel, PhotoViewViewModelArgs>(
                    new PhotoViewViewModelArgs
                    {
                        InterviewId = this.interviewId,
                        FileName = this.GetPictureFileName()
                    });
            }
        });

        public QuestionInstructionViewModel InstructionViewModel { get; }

        private readonly QuestionStateViewModel<PictureQuestionAnswered> questionState;
        public bool IsSignature { get; private set; }
        public IQuestionStateViewModel QuestionState => this.questionState;

        public void Init(string interviewId, 
            Identity entityIdentity, 
            NavigationState navigationState)
        {
            this.questionState.Init(interviewId, entityIdentity, navigationState);
            this.InstructionViewModel.Init(interviewId, entityIdentity, navigationState);
            this.questionIdentity = entityIdentity;

            IStatefulInterview interview = this.interviewRepository.Get(interviewId);
            this.interviewId = interview.Id;

            var questionnaire = this.questionnaireStorage.GetQuestionnaire(interview.QuestionnaireIdentity, interview.Language);
            this.variableName = questionnaire.GetQuestionVariableName(entityIdentity.Id);
            this.IsSignature = questionnaire.IsSignature(entityIdentity.Id);
            var multimediaQuestion = interview.GetMultimediaQuestion(entityIdentity);
            if (multimediaQuestion.IsAnswered())
            {
                var multimediaAnswer = multimediaQuestion.GetAnswer();
                this.Answer =  this.imageFileStorage.GetInterviewBinaryData(this.interviewId, multimediaAnswer.FileName);
            }

            this.eventRegistry.Subscribe(this, interviewId);
        }

        private async Task RequestAnswerAsync()
        {
            var pictureFileName = this.GetPictureFileName();

            if (this.IsSignature)
            {
                if (this.Answer?.Length > 0)
                {
                    this.StorePictureFile(new MemoryStream(this.Answer), pictureFileName);

                    var command = new AnswerPictureQuestionCommand(
                        this.interviewId,
                        this.userId,
                        this.questionIdentity.Id,
                        this.questionIdentity.RosterVector,
                        pictureFileName);

                    try
                    {
                        await this.Answering.SendQuestionCommandAsync(command);
                        await this.QuestionState.Validity.ExecutedWithoutExceptions();
                    }
                    catch (InterviewException ex)
                    {
                        await this.imageFileStorage.RemoveInterviewBinaryData(this.interviewId, pictureFileName);
                        await this.QuestionState.Validity.ProcessException(ex);
                    }
                }
            }
            else
            {
                var choosen = await this.userInteractionService.SelectOneOptionFromList(
                    UIResources.Multimedia_PictureSource, new[]
                    {
                        UIResources.Multimedia_TakePhoto,
                        UIResources.Multimedia_PickFromGallery
                    });

                try
                {
                    Stream pictureStream = null;
                    if (choosen == UIResources.Multimedia_TakePhoto)
                    {
                        pictureStream = await this.pictureChooser.TakePicture();
                    }
                    else if (choosen == UIResources.Multimedia_PickFromGallery)
                    {
                        pictureStream = await this.pictureChooser.ChoosePictureGallery();
                    }

                    if (pictureStream != null)
                    {
                        using (pictureStream)
                        {
                            this.StorePictureFile(pictureStream, pictureFileName);

                            var command = new AnswerPictureQuestionCommand(
                                this.interviewId,
                                this.userId,
                                this.questionIdentity.Id,
                                this.questionIdentity.RosterVector,
                                pictureFileName);

                            try
                            {
                                await this.Answering.SendQuestionCommandAsync(command);
                                this.Answer =
                                    await this.imageFileStorage.GetInterviewBinaryDataAsync(this.interviewId,
                                        pictureFileName);
                                await this.QuestionState.Validity.ExecutedWithoutExceptions();
                            }
                            catch (InterviewException ex)
                            {
                                await this.imageFileStorage.RemoveInterviewBinaryData(this.interviewId, pictureFileName);
                                await this.QuestionState.Validity.ProcessException(ex);
                            }
                        }
                    }
                }
                catch (MissingPermissionsException e) when (e.PermissionType == typeof(CameraPermission))
                {
                    await this.QuestionState.Validity.MarkAnswerAsNotSavedWithMessage(UIResources
                        .MissingPermissions_Camera);
                }
                catch (MissingPermissionsException e) when (e.PermissionType == typeof(StoragePermission))
                {
                    await this.QuestionState.Validity.MarkAnswerAsNotSavedWithMessage(UIResources
                        .MissingPermissions_Storage);
                }
                catch (MissingPermissionsException mpe)
                {
                    await this.QuestionState.Validity.MarkAnswerAsNotSavedWithMessage(mpe.Message);
                }
            }
        }

        private async Task RemoveAnswerAsync()
        {
            try
            {
                var pictureFileName = this.GetPictureFileName();
                await this.imageFileStorage.RemoveInterviewBinaryData(this.interviewId, pictureFileName);

                await this.Answering.SendQuestionCommandAsync(
                    new RemoveAnswerCommand(this.interviewId,
                        this.userId,
                        this.questionIdentity));
                await this.QuestionState.Validity.ExecutedWithoutExceptions();
            }
            catch (InterviewException exception)
            {
                await this.QuestionState.Validity.ProcessException(exception);
            }
        }

        public void Handle(AnswersRemoved @event)
        {
            foreach (var question in @event.Questions)
            {
                if (this.questionIdentity.Equals(question.Id, question.RosterVector))
                {
                    this.Answer = null;
                    this.imageFileStorage.RemoveInterviewBinaryData(this.interviewId, this.GetPictureFileName());
                }
            }
        }

        private void StorePictureFile(Stream pictureStream, string pictureFileName)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                pictureStream.CopyTo(ms);
                byte[] pictureBytes = ms.ToArray();
                this.imageFileStorage.StoreInterviewBinaryData(this.interviewId, pictureFileName, pictureBytes, null);
            }
        }

        private string GetSignaturePointsFileName() => $"{this.variableName}__{this.questionIdentity.RosterVector}__signature.json";
        private string GetPictureFileName() => AnswerUtils.GetPictureFileName(this.variableName, this.questionIdentity.RosterVector);

        public void Dispose()
        {
            this.eventRegistry.Unsubscribe(this);
            this.QuestionState.Dispose();
            this.InstructionViewModel.Dispose();
        }
    }
}
