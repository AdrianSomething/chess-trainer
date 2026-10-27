using ChessOpeningTrainer;
using System.Text.Json.Nodes;
internal static class Version271Checks
{
 public static void Run(string root)
 {
  var start=Position.FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");
  foreach(var (white,from,to,rookFrom,rookTo) in new[]{(true,4,6,7,5),(true,4,2,0,3),(false,60,62,63,61),(false,60,58,56,59)})
  {
   Program.Check(PreferredMovePlan.CanAppend(start,white,from,to),"Geometric preference permits castling");
   var after=PreferredMovePlan.Preview(start,[new(from,to,white?'K':'k')]);
   Program.Check(after.Squares[rookFrom]=='\0'&&after.Squares[rookTo]==(white?'R':'r'),"Castling preference moves the rook");
   Program.Check(!after.Castling.Contains(white?'K':'k')&&!after.Castling.Contains(white?'Q':'q'),"Castling consumes both rights for that side");
  }
  var moved=PreferredMovePlan.Preview(start,[new(7,15,'R'),new(15,7,'R')]);
  Program.Check(!PreferredMovePlan.CanAppend(moved,true,4,6),"Returning rook does not restore a castling right");
  var blocked=start.Clone();blocked.Squares[5]='B';Program.Check(!PreferredMovePlan.CanAppend(blocked,true,4,6),"Own bishop blocks planned castling");
  var opening=new OpeningDefinition{Perspective="black",GenerationStartFen=Position.InitialFen,Tree=new()};
  var a=new OpeningNode();var b=new OpeningNode();opening.Tree.Moves.Add(new(){Uci="e2e4",Next=a});a.Moves.Add(new(){Uci="e7e5",Next=b});
  b.Moves.Add(new(){Uci="g1f3",Next=new()});b.Moves.Add(new(){Uci="f1c4",Next=new()});
  var report=OpeningCoverage.Calculate(opening,p=>p.ToFen()==Position.InitialFen?[("d2d4",new(900,0,0,0))]:p.Fullmove==2&&p.WhiteToMove?[("g1f3",new(60,0,0,0)),("f1c4",new(30,0,0,0)),("b1c3",new(10,0,0,0))]:[],default);
  Program.Check(report.Score==90&&report.Gaps.Single().Move=="b1c3","Fixed e4/e5 prefix is excluded even with initial GenerationStartFen");
  Program.Check(LabConfiguration.Value(JsonValue.Create(42))==42&&LabConfiguration.Value(JsonValue.Create(.25))==.25,"JSON integer/float constants share the curve editor");
  foreach(string mode in new[]{"evolution","distillation","selfplay","neat"})
  {
   var plan=LabConfiguration.Minimal(mode);Program.Check(plan["mode"]!.GetValue<string>()==mode&&plan["rounds"]!.GetValue<int>()==3,"Minimal training template "+mode);
   using var form=new EngineLabForm(Path.Combine(root,"lab-"+mode));form.CreateControl();Program.Check(form.Controls.Count>0,"Engine Lab constructs without external engines or Python");
  }
 }
}
