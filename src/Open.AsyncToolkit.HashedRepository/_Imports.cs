global using System.Buffers;
global using System.Diagnostics.Contracts;
global using System.Text;
global using Open.AsyncToolkit.KeyValue;
global using Open.AsyncToolkit.BlobStorage;

#if NET9_0_OR_GREATER
global using System.Collections.Frozen;
#else
global using System.Collections.Immutable;
#endif