using OmniSharp.Extensions.LanguageServer.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldBoxModdingToolChain.Utils
{
    public class Buffer
    {
        public string Text { get; }
        public int Version { get; }
        public List<string> Lines { get; }

        public Buffer(string text, int version)
        {
            Text = text;
            Version = version;
            Lines = new List<string>(text.Split(new[] { "\n" }, StringSplitOptions.None));
        }
    }

    public class BufferManager
    {
        private readonly ConcurrentDictionary<DocumentUri, Buffer> _buffers = new ConcurrentDictionary<DocumentUri, Buffer>();

        public void UpdateBuffer(DocumentUri documentPath, string content)
        {
            var buffer = new Buffer(content, GetBuffer(documentPath)?.Version + 1 ?? 1);
            _buffers.AddOrUpdate(documentPath, buffer, (_, _) => buffer);
        }

        public Buffer GetBuffer(DocumentUri documentPath)
        {
            return _buffers.TryGetValue(documentPath, out var buffer) ? buffer : null;
        }

        public bool RemoveBuffer(DocumentUri documentPath)
        {
            return _buffers.TryRemove(documentPath, out _);
        }
    }
}
