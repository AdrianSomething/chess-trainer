using System.Text.Json.Nodes;
namespace ChessOpeningTrainer;

/// <summary>UI-independent, JSON-compatible experiment templates. Python reads the same fields.</summary>
public static class LabConfiguration
{
    public static JsonObject Curve(double value, params (double X,double Y)[] points) => new() { ["value"]=value,["cycle"]=false,["points"]=new JsonArray(points.Select(p=>(JsonNode)new JsonArray(p.X,p.Y)).ToArray()) };
    public static JsonObject Task(string kind, string name, bool enabled=true) => new() { ["kind"]=kind,["name"]=name,["enabled"]=enabled,["parameters"]=new JsonObject() };
    public static JsonObject Default(string mode="evolution")
    {
        var mutation=Curve(.1,(1,.15),(20,.015),(25,.015));mutation["cycle"]=true;
        var survivors=Curve(.05,(1,.05),(20,.2),(25,.2));survivors["cycle"]=true;
        var schedules=new JsonObject {["population"]=Curve(100),["mutation"]=mutation,["survivors"]=survivors,["duplication_start"]=Curve(1,(0,1),(1,1)),["duplication_end"]=Curve(1,(0,1),(1,1))};
        var tournament=Task("tournament","Engine-Turnier");tournament["parameters"]=new JsonObject{["count"]=Curve(10),["pairing"]="random",["max_moves"]=Curve(500),["win"]=Curve(100,(0,100),(1000,90)),["move"]=Curve(.002),["loss"]=Curve(-30),["draw"]=Curve(-5),["abort"]=Curve(-15),["weight"]=Curve(1)};
        var sf=Task("stockfish","Gegen Stockfish");sf["parameters"]=new JsonObject{["count"]=Curve(6),["depth"]=Curve(1,(1,1),(101,11)),["intelligence"]=Curve(100),["weight"]=Curve(1)};
        var mate=Task("mate","Mattaufgaben");mate["parameters"]=new JsonObject{["count"]=Curve(10),["min_n"]=Curve(1),["max_n"]=Curve(5),["distribution_start"]=Curve(1,(0,1),(1,1)),["distribution_end"]=Curve(1,(0,1),(1,1)),["source"]="selfplay",["solution_reward"]=Curve(20),["weight"]=Curve(1)};
        var plan=new JsonObject { ["mode"]=mode,["model"]="compact",["device"]="cuda",["rounds"]=100,["runs"]=1,["population"]=100,["display_boards"]=1,["visualize"]=true,["record_thinking"]=true,["vram_gib"]=7.5,["batch_size"]=32,
            ["schedules"]=schedules,["tasks"]=mode=="evolution"?new JsonArray(tournament,sf,mate):new JsonArray(Task(mode=="selfplay"?"selfplay":"distillation",mode=="selfplay"?"Actor-Critic-Selbstspiel":"Stockfish-Imitation")),
            ["results"]=new JsonArray(Task("benchmark","Stockfish-Tiefenvergleich"),Task("speed","Rechengeschwindigkeit")) };
        if(mode=="neat") { plan["model"]="neat";plan["device"]="cpu";plan["schedules"]!["population"]=Curve(20);plan["tasks"]=new JsonArray(tournament.DeepClone()); }
        return plan;
    }
    public static JsonObject Minimal(string mode)
    {
        var plan=Default(mode);plan["rounds"]=3;plan["population"]=4;plan["max_moves"]=12;plan["results"]=new JsonArray(Task("speed","Rechengeschwindigkeit"));
        plan["schedules"]!["population"]=Curve(mode=="neat"?8:4);
        var task=Task(mode switch{"evolution" or "neat"=>"tournament","selfplay"=>"selfplay",_=>"distillation"},mode switch{"evolution" or "neat"=>"Kurzes Turnier","selfplay"=>"Kurzes Selbstspiel",_=>"Kurze Lehrer-Imitation"});
        task["parameters"]=new JsonObject{["count"]=Curve(mode=="distillation"?16:2)};
        if(mode=="distillation"){task["parameters"]!["depth"]=Curve(1);task["parameters"]!["batch_size"]=Curve(4);}
        else task["parameters"]!["max_moves"]=Curve(12);
        plan["tasks"]=new JsonArray(task);return plan;
    }
    public static double Value(JsonNode? node,double fallback=0)
    {
        if(node is JsonObject curve)return Value(curve["value"],fallback);
        if(node is null)return fallback;
        return double.TryParse(node.ToJsonString(),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var number)?number:fallback;
    }
}
