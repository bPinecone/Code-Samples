using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerFrontend
{
    public enum PacketType
    {
        NULL,
        ServerMsg,
        Login,
        CharacterInfo,
        PositionUpdate,
        ChangeMap,
        HitboxCreate,
        ProjectileCreate,
        Damage,
        CrowdControl,
        Death,
        ItemRemove,
        ItemAdd,
        GetEquipment,
        SetEquipment,
        GetInventory,
        SERVER_QueryLogin,
        SERVER_QueryInv,
        SERVER_QueryEquip
    }

    public enum NetDataType
    {
        nBool,
        nChar,
        nInt,
        nFloat,
        nString
    }
}
