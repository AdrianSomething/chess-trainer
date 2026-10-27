"""Three explicit model profiles. Memory estimates include AdamW training state."""
import torch
from torch import nn

PROFILES = {
    "compact": dict(title="Compact GPU", widths=[128], estimated_training_gib=.1),
    "balanced-8gb": dict(title="Balanced GPU (8 GB card)", widths=[4096,2048], estimated_training_gib=1.4),
    "backprop-8gb": dict(title="Backpropagation GPU (8 GB card)", widths=[15360,15360,4096], estimated_training_gib=7.0),
}

class ChessNet(nn.Module):
    def __init__(self, profile="compact", device=None):
        super().__init__()
        if profile not in PROFILES: raise ValueError("Unknown model profile.")
        widths=PROFILES[profile]["widths"]
        layers=[nn.Conv2d(17,32,3,padding=1,device=device),nn.ReLU(),nn.Conv2d(32,32,3,padding=1,device=device),nn.ReLU(),nn.Flatten()]
        last=2048
        for width in widths:
            layers.extend([nn.Linear(last,width,device=device),nn.ReLU()]);last=width
        self.body=nn.Sequential(*layers)
        self.policy=nn.Linear(last,20480,device=device)
        self.value=nn.Sequential(nn.Linear(last,1,device=device),nn.Tanh())
    def forward(self, positions):
        hidden=self.body(positions)
        return self.policy(hidden),self.value(hidden).squeeze(-1)

def parameter_bytes(profile):
    model=ChessNet(profile,device="meta")
    return sum(p.numel()*4 for p in model.parameters())

def requirements(profile, training=True):
    weights=parameter_bytes(profile)
    # Parameters + gradients + two Adam moments, plus activations/workspaces/context margin.
    return dict(weights_bytes=weights, gpu_bytes=(weights*4+512*1024**2 if training else weights+256*1024**2))
