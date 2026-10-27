"""Fast deterministic checks; no GPU, network, weights or chess engine required."""
import random
import unittest
from collections import Counter
from config import Config
from schedule import pairings, selection, reward

class ScheduleChecks(unittest.TestCase):
    def test_balanced_random_tournament(self):
        for size in (2, 3, 100):
            games = pairings(size, 10, random.Random(270))
            self.assertEqual(len(games), size * 5)
            self.assertTrue(all(a != b for a, b in games))
            self.assertEqual(Counter(a for a, _ in games), Counter({i: 5 for i in range(size)}))
            self.assertEqual(Counter(b for _, b in games), Counter({i: 5 for i in range(size)}))

    def test_selection_cycle(self):
        c = Config()
        self.assertEqual(selection(1, c), selection(26, c))
        self.assertEqual(selection(1, c)[0], 5)
        self.assertEqual(selection(25, c)[0], 20)
        self.assertLess(selection(25, c)[1], selection(1, c)[1])

    def test_fast_win_and_limit_penalty(self):
        c = Config()
        early = reward("1-0", True, 10, "checkmate", c) + 10*c.survival_reward
        late = reward("1-0", True, 100, "checkmate", c) + 100*c.survival_reward
        self.assertGreater(early, late)
        self.assertLess(reward("1/2-1/2", True, 1000, "move_limit", c), reward("1/2-1/2", True, 10, "draw", c))

    def test_reject_unbalanced_or_excessive_configuration(self):
        Config().validate()
        for c in (Config(peer_games=3), Config(vram_gib=9), Config(population=1000)):
            with self.assertRaises(ValueError): c.validate()

if __name__ == "__main__":
    unittest.main()
