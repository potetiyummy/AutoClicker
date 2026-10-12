import os
import shutil
import threading
from pynput.keyboard import Listener, Key
from engine import MacroEngine
from gui import AutoClickerApp

def cleanup_pycache():
    """実行フォルダ直下、およびguiフォルダ内の__pycache__を自動で消去する"""
    base_dir = os.path.dirname(os.path.abspath(__file__))
    targets = [
        os.path.join(base_dir, "__pycache__"),
        os.path.join(base_dir, "gui", "__pycache__"),
        os.path.join(base_dir, "gui", "tabs", "__pycache__")
    ]
    for path in targets:
        if os.path.exists(path) and os.path.isdir(path):
            try:
                shutil.rmtree(path)
            except Exception:
                pass

def main():
    engine = MacroEngine()
    app = AutoClickerApp(engine)
    engine.set_ui(app)

    def on_press(key):
        if key == Key.f8:
            engine.toggle()

    Listener(on_press=on_press).start()
    threading.Thread(target=engine.loop, daemon=True).start()
    
    try:
        app.mainloop()
    finally:
        cleanup_pycache()

if __name__ == "__main__":
    main()
