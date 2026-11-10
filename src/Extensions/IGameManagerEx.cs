using AmongUs.GameOptions;

namespace Lotus.Extensions
{
    public static class IGameManagerEx
    {
        public static void Set(this BoolOptionNames name, bool value, IGameOptions opt) => opt.SetBool(name, value);
        public static void Set(this BoolOptionNames name, bool value, NormalGameOptionsV12 opt) => opt.SetBool(name, value);
        public static void Set(this BoolOptionNames name, bool value, HideNSeekGameOptionsV12 opt) => opt.SetBool(name, value);

        public static void Set(this Int32OptionNames name, int value, IGameOptions opt) => opt.SetInt(name, value);
        public static void Set(this Int32OptionNames name, int value, NormalGameOptionsV12 opt) => opt.SetInt(name, value);
        public static void Set(this Int32OptionNames name, int value, HideNSeekGameOptionsV12 opt) => opt.SetInt(name, value);

        public static void Set(this FloatOptionNames name, float value, IGameOptions opt) => opt.SetFloat(name, value);
        public static void Set(this FloatOptionNames name, float value, NormalGameOptionsV12 opt) => opt.SetFloat(name, value);
        public static void Set(this FloatOptionNames name, float value, HideNSeekGameOptionsV12 opt) => opt.SetFloat(name, value);

        public static void Set(this ByteOptionNames name, byte value, IGameOptions opt) => opt.SetByte(name, value);
        public static void Set(this ByteOptionNames name, byte value, NormalGameOptionsV12 opt) => opt.SetByte(name, value);
        public static void Set(this ByteOptionNames name, byte value, HideNSeekGameOptionsV12 opt) => opt.SetByte(name, value);

        public static void Set(this UInt32OptionNames name, uint value, IGameOptions opt) => opt.SetUInt(name, value);
        public static void Set(this UInt32OptionNames name, uint value, NormalGameOptionsV12 opt) => opt.SetUInt(name, value);
        public static void Set(this UInt32OptionNames name, uint value, HideNSeekGameOptionsV12 opt) => opt.SetUInt(name, value);
    }
}