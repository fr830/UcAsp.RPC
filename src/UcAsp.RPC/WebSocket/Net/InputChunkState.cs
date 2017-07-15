using System;

namespace UcAsp.WebSocket.Net
{
  internal enum InputChunkState
  {
    None,
    Data,
    DataEnded,
    Trailer,
    End
  }
}
