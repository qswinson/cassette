using System;
using System.IO;
using System.Linq;
using Cassette.IO;
using Cassette.Utilities;

namespace Cassette
{
    public class UrlGenerator : IUrlGenerator
    {
        readonly IUrlModifier urlModifier;
        readonly string cassetteHandlerPrefix;
        readonly bool isFileNameWithHashDisabled;
        readonly IDirectory sourceDirectory;

        public UrlGenerator(IUrlModifier urlModifier, IDirectory sourceDirectory, string cassetteHandlerPrefix, bool isFileNameWithHashDisabled = false)
        {
            this.urlModifier = urlModifier;
            this.cassetteHandlerPrefix = cassetteHandlerPrefix;
            this.isFileNameWithHashDisabled = isFileNameWithHashDisabled;
            this.sourceDirectory = sourceDirectory;
        }

        public string CreateBundleUrl(Bundle bundle)
        {
            var url = cassetteHandlerPrefix + bundle.Url;
            return urlModifier.Modify(url);
        }

        public string CreateAssetUrl(IAsset asset)
        {
            // "~/directory/file.js" --> "cassette.axd/asset/directory/file.js?hash"
            // Asset URLs are only used in debug mode. The hash is placed in the querystring, not the path.
            // This maintains the asset directory structure i.e. two assets in the same directory appear together in web browser JavaScript development tooling.
            
            var assetPath = asset.Path.Substring(1);
            var url = cassetteHandlerPrefix + "asset" + assetPath;

            if (!isFileNameWithHashDisabled)
            {
                var hash = asset.Hash.ToHexString();
                url = url + "?" + hash;
            }

            return urlModifier.Modify(url);
        }

        public string CreateRawFileUrl(string filename)
        {
            if (filename.StartsWith("~") == false)
            {
                throw new ArgumentException("Image filename must be application relative (starting with '~'). Filename: "+filename);
            }

            var file = sourceDirectory.GetFile(filename);
            if (!file.Exists)
            {
                throw new FileNotFoundException("File not found: " + filename, filename);
            }
            using (var stream = file.OpenRead())
            {
                var hash = stream.ComputeSHA1Hash().ToHexString();
                return ((IUrlGenerator)this).CreateRawFileUrl(filename, hash);
            }
        }

        string IUrlGenerator.CreateRawFileUrl(string filename, string hash)
        {
            if (filename.StartsWith("~") == false)
            {
                throw new ArgumentException("Image filename must be application relative (starting with '~'). Filename: "+filename);
            }

            var hashWithPrefix = string.Empty;
            if (!isFileNameWithHashDisabled)
                hashWithPrefix = "-" + hash;

            // "~\example\image.png" --> "/example/image-hash.png"
            var path = ConvertToForwardSlashes(filename).Substring(1);
            path = UriEscapePathSegments(path);
            
            var index = path.LastIndexOf('.');
            if (index >= 0)
            {
                path = path.Insert(index, hashWithPrefix);
            }
            else
            {
                path = path + hashWithPrefix;
            }

            var url = cassetteHandlerPrefix + "file" + path;
            return urlModifier.Modify(url);
        }

        public string CreateCachedFileUrl(string filename)
        {
            if (filename.StartsWith("~") == false)
            {
                throw new ArgumentException("Filename must be application relative (starting with '~'). Filename: "+filename, "filename");
            }

            var path = ConvertToForwardSlashes(filename).Substring(1);
            var url = cassetteHandlerPrefix + "cached" + path;
            return urlModifier.Modify(url);
        }

        public string CreateAbsolutePathUrl(string applicationRelativePath)
        {
            var url = applicationRelativePath.TrimStart('~', '/');
            return urlModifier.Modify(url);
        }

        string ConvertToForwardSlashes(string path)
        {
            return path.Replace('\\', '/');
        }

        /// <summary>
        /// Escape each part of the URL between the slashes.
        /// </summary>
        string UriEscapePathSegments(string path)
        {
            return string.Join(
                "/", 
                path.Split('/').Select(Uri.EscapeDataString).ToArray()
            );
        }
    }
}