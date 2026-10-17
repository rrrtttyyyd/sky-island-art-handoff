using System;
using System.IO;
using UnityEngine;
using SkyIsland.ContractDraftR1;
public static class ContractReview {
    public static void Validate(){
        int count=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL "+name);count++;};
        const long max=9007199254740991L;
        var input=JsonUtility.FromJson<ResultEnvelope>(File.ReadAllText("../contracts/reviews/r1-js-fixture.json"));
        check(input.requestId=="11111111-2222-4333-8444-555555555555","request ID");
        check(input.status=="OK"&&input.data.kind=="inventory","result discriminator");
        check(input.revision==max,"safe integer revision");
        check(input.data.items.Length==2,"inventory array");
        check(input.data.items[0].amount==max&&input.data.items[0].itemId=="wood","long item amount");
        check(input.data.items[1].amount==0,"zero amount");
        check(input.data.consumed.Length==0,"empty array");
        var output=JsonUtility.ToJson(input,true);
        var again=JsonUtility.FromJson<ResultEnvelope>(output);
        check(again.data.items[0].amount==max&&again.revision==max,"CSharp roundtrip");
        var command=new CommandEnvelope{requestId=input.requestId,type="placeFoundation",islandId="isl_test",expectedRevision=17,payload=new CommandPayload{q=-100000,r=100000,foundationTypeId="basic"}};
        var restored=JsonUtility.FromJson<CommandEnvelope>(JsonUtility.ToJson(command));
        check(restored.payload.q==-100000&&restored.payload.r==100000,"signed coordinates");
        check(restored.expectedRevision==17&&restored.payload.foundationTypeId=="basic","typed payload");
        var snapshot=new SnapshotMessage{type="snapshot",islandId="isl_test",revision=19,generatorVersion="v1",generatorSupported=true,permissions=new SnapshotPermissions{canView=true,canEdit=false,canReadInventory=false},foundations=new[]{new FoundationEntry{foundationId="fnd_test",q=0,r=0,foundationTypeId="basic"}},buildings=new BuildingEntry[0]};
        var json=JsonUtility.ToJson(snapshot);var copy=JsonUtility.FromJson<SnapshotMessage>(json);
        check(copy.permissions.canView&&!copy.permissions.canEdit&&copy.foundations.Length==1,"snapshot DTO");
        check(!json.Contains("accountId")&&!json.Contains("items")&&!json.Contains("credential"),"snapshot fields exclude account data");
        Directory.CreateDirectory("../evidence/contracts");
        File.WriteAllText("../evidence/contracts/r1-csharp-output.json",output);
        File.WriteAllText("../evidence/contracts/r1-csharp-check.txt",$"PASS {count} assertions; Unity {Application.unityVersion}; UTC {DateTime.UtcNow:o}\nDTO Editor serialization only, not raw input validation, IL2CPP or production authority.\n");
        Debug.Log("SKY_CONTRACT_R1_PASS "+count);
    }
}
