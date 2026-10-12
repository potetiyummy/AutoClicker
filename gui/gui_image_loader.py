import os
import sys
import tkinter as tk

class ImageLoader:
    @staticmethod
    def load_action_images():
        """imagesフォルダから全7種類のアクション用画像を読み込む"""
        if getattr(sys, 'frozen', False):
            base_path = sys._MEIPASS
        else:
            # guiフォルダーの2つ上のルートディレクトリを取得
            base_path = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
            
        images_dir = os.path.join(base_path, "images")
        image_files = {
            "left": "left_click.png", 
            "right": "right_click.png",
            "drag_straight": "drag_straight.png", 
            "drag_via": "drag_via.png",
            "text": "text.png", 
            "hotkey": "hotkey.png", 
            "wait": "wait.png"
        }
        
        img_cache = {}
        for key, filename in image_files.items():
            img_path = os.path.join(images_dir, filename)
            if os.path.exists(img_path):
                img_cache[key] = tk.PhotoImage(file=img_path)
        return img_cache
