using System;
using Valora.Domain.Enums;

namespace Valora.Application.Common.Events;

public record BatchJobCompletedEvent(Guid JobId, BatchJobType JobType, string Target) : IDomainEvent;
public record BatchJobFailedEvent(Guid JobId, BatchJobType JobType, string Target, string ErrorMessage) : IDomainEvent;
public record AiAnalysisCompletedEvent(string UserId, string Query) : IDomainEvent;
