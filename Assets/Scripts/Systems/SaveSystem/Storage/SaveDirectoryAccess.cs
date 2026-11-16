using System.IO;

internal static class SaveDirectoryAccess {
    public static bool Exists(string directory) {
        var root = Path.GetFullPath(directory);
        var path = root;
        // A missing path is usable only if its nearest existing ancestor is a directory.
        while (path != null) {
            try {
                if ((File.GetAttributes(path) & FileAttributes.Directory) == 0)
                    throw new IOException("The save path contains a file instead of a directory: " + path);
                return path == root;
            } catch (DirectoryNotFoundException) {
            } catch (FileNotFoundException) {
            }
            path = Path.GetDirectoryName(path);
        }
        throw new DirectoryNotFoundException("The save directory root is not available: " + root);
    }
}
