"""Dependency-free schedules, templates and portable NEAT policy regressions."""
import json,unittest
from pathlib import Path
from curves import value,blend_distribution,validate
from neat_policy import PortableNetwork,action_score
from config import Config
class Curves(unittest.TestCase):
 def test_interpolation_and_repeat(self):
  c=dict(value=0,points=[[1,2],[5,10]],cycle=False)
  self.assertEqual(value(c,3),6);self.assertEqual(value(c,9),10)
  c["cycle"]=True;self.assertEqual(value(c,6),2)
 def test_reject_duplicate_or_nan(self):
  for c in (dict(points=[[1,2],[1,3]]),dict(value=float('nan'))):
   with self.assertRaises(ValueError):validate(c)
 def test_distribution_interpolates(self):
  a=dict(points=[[0,1],[1,0]]);b=dict(points=[[0,0],[1,1]])
  self.assertAlmostEqual(blend_distribution(a,b,.25,0),.75)
 def test_action_factorization_covers_all_promotions(self):
  self.assertEqual(len({(i//320,(i//5)%64,i%5) for i in range(20480)}),20480)
  scores=[0.]*133;scores[12]=2;scores[64+28]=3;scores[128]=4
  self.assertEqual(action_score(scores,(12*64+28)*5),9)
 def test_portable_network(self):
  data=dict(format='cot-neat-v1',inputs=list(range(-69,0)),outputs=list(range(133)),nodes=[[0,0.,1.,[[-69,1.]]]])
  network=PortableNetwork(data);self.assertGreater(network.activate([1.]+[0.]*68)[0],.9)
 def test_templates(self):
  for path in Path(__file__).parent.glob('minimal-*.json'):
   c=Config(**json.loads(path.read_text(encoding='utf-8')));c.validate();self.assertEqual(c.rounds,3)
if __name__=='__main__':unittest.main()
