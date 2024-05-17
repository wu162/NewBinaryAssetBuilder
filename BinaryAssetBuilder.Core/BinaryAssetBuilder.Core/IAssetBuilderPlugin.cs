namespace BinaryAssetBuilder.Core
{
	public interface IAssetBuilderPlugin : IAssetBuilderPluginBase
	{
		AssetBuffer ProcessInstance(InstanceDeclaration instance);

		uint GetAllTypesHash();

		uint GetVersionNumber();

		ExtendedTypeInformation GetExtendedTypeInformation(uint typeId);
	}
}
