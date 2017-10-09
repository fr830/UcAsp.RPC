
namespace UcAsp.RPC.ProtoBuf.Compiler
{
    internal delegate void ProtoSerializer(object value, ProtoWriter dest);
    internal delegate object ProtoDeserializer(object value, ProtoReader source);
}
