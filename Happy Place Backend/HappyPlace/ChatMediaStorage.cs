namespace HappyWorld.HappyPlace;

public static class ChatMediaStorage {
    // Fields

    private static readonly string DefaultRootPath = Path.Combine(AppContext.BaseDirectory, "ChatMedia");
    private static string _rootPathOverride;

    // Methods

    public static void SetRootPath(string rootPath) => _rootPathOverride = rootPath;

    public static string SaveFile(Guid assetId, byte[] bytes) {
        Directory.CreateDirectory(RootPath);
        string fileName = assetId.ToString("N") + ".bin";
        File.WriteAllBytes(Path.Combine(RootPath, fileName), bytes);
        return fileName;
    }

    public static byte[] ReadFile(string fileName) {
        string fullPath = FullPath(fileName);
        if (!File.Exists(fullPath))
            return null;
        return File.ReadAllBytes(fullPath);
    }

    public static string FullPath(string fileName) {
        return Path.Combine(RootPath, fileName);
    }

    public static bool FileExists(string fileName) {
        return File.Exists(FullPath(fileName));
    }

    public static void DeleteFile(string fileName) {
        string fullPath = FullPath(fileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public static void ClearAll() {
        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, true);
    }

    // Helpers

    private static string RootPath => _rootPathOverride ?? DefaultRootPath;
}
