namespace EcoSyncWeb.Services
{
    public sealed class AppStoragePaths
    {
        public AppStoragePaths(string? dataDirectory, string webRootPath)
        {
            DataDirectory = dataDirectory;
            WebRootPath = webRootPath;
        }

        public string? DataDirectory { get; }

        public string WebRootPath { get; }

        public string ProjectImagesDirectory => DataDirectory is null
            ? Path.Combine(WebRootPath, "uploads", "projects")
            : Path.Combine(DataDirectory, "uploads", "projects");

        public string? PersistentUploadsDirectory => DataDirectory is null
            ? null
            : Path.Combine(DataDirectory, "uploads");
    }
}
