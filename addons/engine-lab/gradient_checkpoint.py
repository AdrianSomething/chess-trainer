"""Round-atomic tensor-only checkpoints for gradient-based trainers."""
import random
import torch
from evolution import atomic_save


def optimizer_for(policy, rate):
    return torch.optim.AdamW(policy.net.parameters(), lr=rate,
                            fused=policy.device.type=="cuda", foreach=False if policy.device.type!="cuda" else None)


def restore(policy, optimizer, output, resume, rng):
    checkpoint=output/"gradient-checkpoint.pt"
    if resume and checkpoint.exists():
        saved=torch.load(checkpoint,map_location="cpu",weights_only=True,mmap=True)
        if saved.get("model","compact")!=policy.config.model: raise ValueError("Continue with the original architecture.")
        policy.net.load_state_dict(saved["weights"]);optimizer.load_state_dict(saved["optimizer"])
        rng.setstate(saved["rng"]);torch.set_rng_state(saved["torch_rng"])
        if policy.device.type=="cuda" and "cuda_rng" in saved: torch.cuda.set_rng_state(saved["cuda_rng"],policy.device)
        return saved["next_round"],saved["history"]
    if resume and (output/"best.pt").exists():policy.load("resume",output/"best.pt")
    return 1,[]


def save(policy, optimizer, output, round_number, history, rng):
    def cpu(value):
        if torch.is_tensor(value): return value.detach().to("cpu",copy=True)
        if isinstance(value,dict): return {k:cpu(v) for k,v in value.items()}
        if isinstance(value,list): return [cpu(v) for v in value]
        return value
    weights=policy.cpu_weights()
    atomic_save(weights,output/"best.pt")
    saved=dict(weights=weights,optimizer=cpu(optimizer.state_dict()),next_round=round_number+1,
               model=policy.config.model,history=history,rng=rng.getstate(),torch_rng=torch.get_rng_state())
    if policy.device.type=="cuda":saved["cuda_rng"]=torch.cuda.get_rng_state(policy.device)
    atomic_save(saved,output/"gradient-checkpoint.pt")
