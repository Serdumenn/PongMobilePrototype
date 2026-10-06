using System;
using UnityEngine.Scripting;

[Serializable]
[Preserve]
public sealed class FriendInvite
{
    public const string InviteKind = "invite";
    public const int Version = 1;

    public string kind;
    public int v;
    public string code;
    public string mode;

    [Preserve]
    public FriendInvite()
    {
    }

    public static FriendInvite For(string code, string mode)
    {
        return new FriendInvite { kind = InviteKind, v = Version, code = SessionCode.Normalize(code), mode = mode };
    }

    public bool IsValid => kind == InviteKind && v == Version && SessionCode.IsValid(code) && !string.IsNullOrEmpty(mode);
}
