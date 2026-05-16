using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;

namespace AMIS.Playground.Blazor.Services;

internal interface IOrganizationProfileState
{
    OrganizationProfileDto? Profile { get; }
    event Action? OnProfileChanged;
    void SetProfile(OrganizationProfileDto? profile);
}

internal sealed class OrganizationProfileState : IOrganizationProfileState
{
    public OrganizationProfileDto? Profile { get; private set; }
    public event Action? OnProfileChanged;

    public void SetProfile(OrganizationProfileDto? profile)
    {
        Profile = profile;
        OnProfileChanged?.Invoke();
    }
}
