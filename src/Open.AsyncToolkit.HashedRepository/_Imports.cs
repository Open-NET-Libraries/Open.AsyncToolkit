global using Open.AsyncToolkit.KeyValue;
global using System.Buffers;

#if NET9_0_OR_GREATER
global using System.Collections.Frozen;
#else
global using System.Collections.Immutable;
#endif