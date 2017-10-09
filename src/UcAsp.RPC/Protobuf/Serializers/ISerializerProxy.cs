#if !NO_RUNTIME

namespace UcAsp.RPC.ProtoBuf.Serializers
{
    interface ISerializerProxy
    {
        IProtoSerializer Serializer { get; }
    }
}
#endif