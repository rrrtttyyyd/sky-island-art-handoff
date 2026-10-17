using System;
namespace SkyIsland.ContractDraftR1 {



// 数值契约见 §6：amount/revision 用 long（≤ 2^53-1），q/r 用 int（±100000）

[Serializable] public class ItemAmount {
    public string itemId;
    public long amount;
}

[Serializable] public class HelloMessage {
    public string type;              // "hello"
    public int protocolVersion;
    public string buildVersion;
    public string clientNonce;
}

[Serializable] public class AuthMessage {
    public string type;              // "auth"
    public string requestId;
    public string credential;        // 占位符；真实凭证不入任何文件/URL/日志
}

[Serializable] public class AuthResult {
    public string type;              // "authResult"
    public string requestId;
    public string status;            // "OK"
    public string accountId;
    public string sessionId;
    public int serverProtocolVersion;
    public string echoedClientNonce;
}

[Serializable] public class CommandPayload {
    // placeFoundation 用 q/r/foundationTypeId；readInventory 全部留默认值（服务端按 type 忽略）
    public int q;
    public int r;
    public string foundationTypeId;
}

[Serializable] public class CommandEnvelope {
    public string requestId;         // UUID v4
    public string type;              // 白名单： placeFoundation / readInventory
    public string islandId;          // readInventory 可留空
    public long expectedRevision;    // 变更类命令必填
    public CommandPayload payload;
}

[Serializable] public class ResultData {
    public string kind;              // "placeFoundation" | "inventory"
    public string foundationId;
    public ItemAmount[] consumed;
    public string accountId;
    public ItemAmount[] items;
}

[Serializable] public class ErrorBody {
    public string code;
    public string message;
    public long currentRevision;
}

[Serializable] public class ResultEnvelope {
    public string requestId;
    public string status;            // "OK" | "ERROR" | "IN_FLIGHT"
    public long revision;
    public ResultData data;          // status == "OK" 时非空
    public ErrorBody error;          // status == "ERROR" 时非空
}

[Serializable] public class SnapshotPermissions {
    public bool canView;
    public bool canEdit;
    public bool canReadInventory;    // 仅指"自己账户"
}

[Serializable] public class FoundationEntry {
    public string foundationId;
    public int q;
    public int r;
    public string foundationTypeId;
}

[Serializable] public class SnapshotMessage {
    public string type;              // "snapshot"
    public string islandId;
    public long revision;
    public string generatorVersion;
    public bool generatorSupported;
    public SnapshotPermissions permissions;
    public FoundationEntry[] foundations;
    public BuildingEntry[] buildings;
}

[Serializable] public class BuildingEntry {
    public string buildingId;
    public string foundationId;
    public string buildingTypeId;
}

}