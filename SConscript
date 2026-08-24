Import('env')
from pathlib import Path

src_dir = Path('./src')
vsqa_sources = [
    str(p) for p in src_dir.rglob("*.cs")
]

vsqa_csproj_path = File('TestHarnessMod.csproj').abspath
vsqa_bin_dir     = Dir('TestHarnessMod/bin').abspath
vsqa_tests_dir   = Dir('src/test').abspath

Export('vsqa_sources', 'vsqa_csproj_path', 'vsqa_bin_dir', 'vsqa_tests_dir')
