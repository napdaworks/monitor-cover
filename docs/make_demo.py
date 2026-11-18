# 모니터 가리기 - NAPDA (만든 사람 Hononoo)
# 시연 장면 그림(scene1~6.png)을 이어 붙여 GIF와 MP4 시연 영상을 만든다.
# 장면 그림은 "모니터 가리기.exe --doc-images docs/images"로 만든다.
# 필요한 것: Pillow, imageio-ffmpeg (pip install pillow imageio-ffmpeg)

import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg
from PIL import Image

HOLD_SECONDS = [2.6, 2.6, 3.0, 3.0, 3.0, 2.8]
GIF_SIZE = (960, 504)
FPS = 30


def load_scenes(scene_dir):
    return [Image.open(scene_dir / f"scene{i}.png").convert("RGB") for i in range(1, len(HOLD_SECONDS) + 1)]


def make_gif(scenes, out_path):
    frames, durations = [], []
    small = [s.resize(GIF_SIZE, Image.LANCZOS) for s in scenes]
    for i, scene in enumerate(small):
        frames.append(scene)
        durations.append(int(HOLD_SECONDS[i] * 1000))
        following = small[(i + 1) % len(small)]
        for k in range(1, 6):
            frames.append(Image.blend(scene, following, k / 6))
            durations.append(60)
    frames[0].save(out_path, save_all=True, append_images=frames[1:], duration=durations, loop=0, optimize=True)


def make_mp4(scenes, out_path):
    width, height = scenes[0].size
    command = [
        imageio_ffmpeg.get_ffmpeg_exe(), "-y", "-loglevel", "error",
        "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{width}x{height}", "-r", str(FPS), "-i", "-",
        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "22", "-movflags", "+faststart", str(out_path),
    ]
    encoder = subprocess.Popen(command, stdin=subprocess.PIPE)
    for i, scene in enumerate(scenes):
        for _ in range(int(HOLD_SECONDS[i] * FPS)):
            encoder.stdin.write(scene.tobytes())
        following = scenes[(i + 1) % len(scenes)]
        for k in range(1, 13):
            encoder.stdin.write(Image.blend(scene, following, k / 12).tobytes())
    encoder.stdin.close()
    if encoder.wait() != 0:
        sys.exit("ffmpeg failed")


def main():
    images = Path(sys.argv[1] if len(sys.argv) > 1 else "docs/images")
    scenes = load_scenes(images / "scenes")
    make_gif(scenes, images / "demo.gif")
    make_mp4(scenes, images / "demo.mp4")
    for name in ("demo.gif", "demo.mp4"):
        print(name, (images / name).stat().st_size, "bytes")


if __name__ == "__main__":
    main()
