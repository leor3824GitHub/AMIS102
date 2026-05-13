using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.Categories.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : ICommand;

