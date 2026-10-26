"""Small JSON-lines client. The C# host owns all rules, legal moves and game history."""
import json
import subprocess


class Host:
    def __init__(self, executable):
        self.process = subprocess.Popen([str(executable), "--training-host"], stdin=subprocess.PIPE,
                                        stdout=subprocess.PIPE, text=True, encoding="utf-8",
                                        creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
        hello = self.call(op="hello")
        if hello["protocol"] != 1:
            raise RuntimeError("Unsupported training protocol.")
        self.has_teacher = hello["stockfish"]

    def call(self, **request):
        self.process.stdin.write(json.dumps(request, separators=(",", ":")) + "\n")
        self.process.stdin.flush()
        line = self.process.stdout.readline()
        if not line:
            raise RuntimeError("Chess host exited unexpectedly.")
        response = json.loads(line)
        if not response["ok"]:
            raise RuntimeError(response["error"])
        return response["result"]

    def close(self):
        if self.process.poll() is None:
            self.process.stdin.close()
            try:
                self.process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                self.process.kill()
                self.process.wait()
        self.process.stdout.close()

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        self.close()
