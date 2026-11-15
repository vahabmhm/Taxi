using UnityEditor;

namespace EndlessCarChase.EditorTools
{
    /// <summary>
    /// Import settings for the Kenney city kit models ( Assets/ThirdParty/KenneyCity/Resources/KenneyCity ):
    /// no embedded materials ( ECCCityWorld gives each kit one shared material with its color map ), no animations,
    /// cameras or lights. The color maps are flat color cells, so they get no mipmaps ( no color bleeding far away )
    /// </summary>
    public class ECCKenneyCityImport : AssetPostprocessor
    {
        const string Folder = "/KenneyCity/";

        void OnPreprocessModel()
        {
            if (assetPath.Contains(Folder) == false) return;

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
        }

        void OnPreprocessTexture()
        {
            if (assetPath.Contains(Folder) == false) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.mipmapEnabled = false;
        }
    }
}
