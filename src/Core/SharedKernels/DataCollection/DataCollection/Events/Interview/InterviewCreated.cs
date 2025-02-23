﻿using System;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Base;

namespace WB.Core.SharedKernels.DataCollection.Events.Interview
{
    public class InterviewCreated : InterviewActiveEvent
    {
        public Guid QuestionnaireId { get; }
        public long QuestionnaireVersion { get; }

        public int? AssignmentId { get; }
        public bool UsesExpressionStorage { get; }
        public DateTime? CreationTime { get; }
        public bool? IsAudioRecordingEnabled { get; set; }

        public InterviewCreated(Guid userId, Guid questionnaireId, long questionnaireVersion, 
            int? assignmentId, bool? isAudioRecordingEnabled, DateTimeOffset originDate, 
            bool usesExpressionStorage = false)
            : base(userId, originDate)
        {
            this.QuestionnaireId = questionnaireId;
            this.QuestionnaireVersion = questionnaireVersion;
            this.AssignmentId = assignmentId;
            this.IsAudioRecordingEnabled = isAudioRecordingEnabled;
            this.UsesExpressionStorage = usesExpressionStorage;

            if (originDate != default(DateTimeOffset))
                this.CreationTime = originDate.UtcDateTime;
        }
    }
}
