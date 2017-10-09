#if !NO_RUNTIME
using UcAsp.RPC.ProtoBuf.Meta;
namespace UcAsp.RPC.ProtoBuf.Serializers
{
    interface IProtoTypeSerializer : IProtoSerializer
    {
        bool HasCallbacks(TypeModel.CallbackType callbackType);
        bool CanCreateInstance();
#if !FEAT_IKVM
        object CreateInstance(ProtoReader source);
        void Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context);
#endif

        void EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType);

        void EmitCreateInstance(Compiler.CompilerContext ctx);

    }
}
#endif