using AMIS.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace AMIS.Modules.Identity.Contracts.v1.Sessions.GetMySessions;

public sealed record GetMySessionsQuery : IQuery<List<UserSessionDto>>;

