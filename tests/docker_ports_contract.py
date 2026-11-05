"""Static checks for default and configurable host ports (not a Docker runtime test)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
compose = (ROOT / 'compose.yaml').read_text(encoding='utf-8')
program = (ROOT / 'backend/Program.cs').read_text(encoding='utf-8')
admin = (ROOT / 'admin/src/main.jsx').read_text(encoding='utf-8')

def test_ports():
    for name, host, internal in (
        ('ADMIN', 5174, 5174),
        ('WEB', 5175, 5175),
        ('API', 8081, 8080),
        ('DB', 5435, 5432),
    ):
        expected = f'127.0.0.1:${{FOUNDLY_{name}_PORT:-{host}}}:{internal}'
        assert expected in compose, f'Missing mapping {name}: {expected}'
    for name, default in (('Web', 5175), ('Admin', 5174), ('Api', 8081)):
        assert f'AppPorts__{name}' in compose
        assert f'HostPort("AppPorts:{name}", {default})' in program
    assert 'allowedLocalhostPorts.Contains(uri.Port)' in program
    assert 'VITE_FRONTEND_URL' in compose
    assert admin.count('href={FRONTEND_URL}') == 2
    assert "API_PROXY_TARGET: http://api:8080" in compose
    assert 'Host=postgres;Port=5432;' in compose

if __name__ == '__main__':
    test_ports()
    print('PASS configurable Docker host ports source contract')
