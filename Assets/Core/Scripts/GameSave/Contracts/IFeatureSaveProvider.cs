namespace Core.Scripts.GameSave.Contracts
{
    public interface IFeatureSaveProvider
    {
        FeatureSnapshot Capture();
        string FeatureName { get; }
    }
}