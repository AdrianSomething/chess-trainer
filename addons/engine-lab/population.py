"""Disk-backed population: stable IDs and bounded CPU residency, with atomic generation checkpoints."""
import shutil
from pathlib import Path
import torch
from models import ChessNet, parameter_bytes

def save_weights(weights, path):
    temporary=path.with_suffix(".tmp")
    torch.save(weights,temporary)
    temporary.replace(path)

def create_member(directory,identity,profile,parent=None,sigma=.1):
    path=directory/f"engine-{identity:08d}.pt"
    if parent is None:
        net=ChessNet(profile)
        save_weights(net.state_dict(),path)
    else:
        source=torch.load(parent,map_location="cpu",weights_only=True,mmap=True)
        # Only one offspring is materialized; never all 100 models in RAM.
        child={}
        for name,tensor in source.items():
            result=tensor.clone(); flat=result.view(-1)
            scale=max(.002,float(tensor.std(unbiased=False)))*sigma
            for start in range(0,flat.numel(),4_000_000):
                chunk=flat[start:start+4_000_000]
                chunk.add_(torch.randn_like(chunk),alpha=scale)
            child[name]=result
        save_weights(child,path)
    return path

def check_space(directory,profile,count):
    required=parameter_bytes(profile)*(count+2)
    free=shutil.disk_usage(directory).free
    if free<required: raise RuntimeError(f"Population/checkpoint needs approximately {required/1024**3:.1f} GiB additional disk space; available {free/1024**3:.1f} GiB.")

def retain_only(directory,referenced):
    keep={Path(p).resolve() for p in referenced}
    for path in directory.glob("engine-*.pt"):
        if path.resolve() not in keep:path.unlink()
