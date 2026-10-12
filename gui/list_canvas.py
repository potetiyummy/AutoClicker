import tkinter as tk
import customtkinter as ctk
from i18n import t as tr

class MacroListCanvas(ctk.CTkFrame):
    def __init__(self, master, app, engine):
        super().__init__(master, fg_color="#2B2B2B", border_width=1, border_color="#3E3E3E")
        self.app = app
        self.engine = engine
        
        self.drag_item_index = None
        self.item_height = 40
        self.insert_index = None
        
        self.resized_images = {}
        self.resize_action_images()
        
        self.canvas = tk.Canvas(self, bg="#2B2B2B", bd=0, highlightthickness=0)
        self.canvas.pack(fill="both", expand=True, padx=5, pady=5)
        
        self.canvas.bind("<Button-1>", self.on_drag_start)
        self.canvas.bind("<B1-Motion>", self.on_dragging)
        self.canvas.bind("<ButtonRelease-1>", self.on_drag_end)

    def resize_action_images(self):
        for key, original_img in self.app.img_cache.items():
            try:
                self.resized_images[key] = original_img.subsample(4, 4)
            except Exception:
                self.resized_images[key] = original_img

    def on_drag_start(self, event):
        clicked_idx = event.y // self.item_height
        if clicked_idx < len(self.engine.actions):
            self.drag_item_index = clicked_idx
            self.canvas.config(cursor="hand2")
            self.draw_list_items(dragging_y=event.y)

    def on_dragging(self, event):
        if self.drag_item_index is None: return
        y = event.y
        max_y = len(self.engine.actions) * self.item_height
        y = max(0, min(y, max_y))
        
        raw_index = (y + (self.item_height // 2)) // self.item_height
        self.insert_index = max(0, min(raw_index, len(self.engine.actions)))
        self.draw_list_items(dragging_y=y)

    def on_drag_end(self, event):
        if self.drag_item_index is None: return
        if self.insert_index is not None:
            target_idx = self.insert_index
            if self.drag_item_index < target_idx: target_idx -= 1
            if target_idx != self.drag_item_index and target_idx >= 0:
                action = self.engine.actions.pop(self.drag_item_index)
                self.engine.actions.insert(target_idx, action)
        self.drag_item_index = None
        self.insert_index = None
        self.canvas.config(cursor="")
        self.draw_list_items()

    def draw_list_items(self, dragging_y=None):
        self.canvas.delete("all")
        width = self.canvas.winfo_width()
        if width <= 0: width = 380
            
        current_y = 0
        for i, a in enumerate(self.engine.actions):
            if self.drag_item_index is not None and i == self.drag_item_index:
                continue
            if self.insert_index is not None and i == self.insert_index:
                self.draw_insertion_lines(current_y, width)
                current_y += self.item_height // 2
                
            self.draw_item_box(i, a, current_y, width)
            current_y += self.item_height
            
        if self.insert_index is not None and self.insert_index == len(self.engine.actions):
            self.draw_insertion_lines(current_y, width)
            
        if self.drag_item_index is not None and dragging_y is not None:
            drag_y_pos = dragging_y - (self.item_height // 2)
            self.draw_item_box(self.drag_item_index, self.engine.actions[self.drag_item_index], drag_y_pos, width, is_dragging=True)

    def draw_insertion_lines(self, y, width):
        accent_color = "#1F6AA5"
        space = 6
        self.canvas.create_line(10, y - space, width - 10, y - space, fill=accent_color, width=2)
        self.canvas.create_line(10, y + space, width - 10, y + space, fill=accent_color, width=2)

    def draw_item_box(self, index, action, y, width, is_dragging=False):
        bg_color = "#3A3A3A" if is_dragging else "#323232"
        outline_color = "#1F6AA5" if is_dragging else "#3E3E3E"
        
        t = action["type"]
        img_key = None
        
        if t == "click":
            img_key = "right" if "right" in action["btn"] else "left"
            text = tr("list_click", index=index+1, btn=tr(action["btn"]), x=action['x'], y=action['y'])
        elif t == "drag":
            wp_count = len(action.get("waypoints", []))
            img_key = "drag_via" if wp_count > 0 else "drag_straight"
            text = tr("list_drag", index=index+1, btn=tr(action["btn"]), sx=action['sx'], count=wp_count, ex=action['ex'])
        elif t == "hold":
            img_key = "right" if "right" in action["btn"] else "left"
            text = tr("list_hold", index=index+1, btn=tr(action["btn"]), x=action['x'], y=action['y'], duration=action['duration'])
        elif t == "keyboard":
            img_key = "text" if action["mode"] == "text" else "hotkey"
            text = tr("list_keyboard", index=index+1, kind=tr("keyboard"), mode=tr(action["mode"]), value=action['value'])
        # 【修正】待機アクション時に、新しく追加された "wait" キーの画像をセット
        elif t == "wait":
            img_key = "wait"
            text = tr("list_wait", index=index+1, kind=tr("wait"), duration=action['duration'])
        
        self.canvas.create_rectangle(5, y + 2, width - 5, y + self.item_height - 2, fill=bg_color, outline=outline_color, width=1)
        
        text_offset = 15
        if img_key in self.resized_images:
            self.canvas.create_image(25, y + (self.item_height // 2), image=self.resized_images[img_key], anchor="center")
            text_offset = 45
            
        self.canvas.create_text(text_offset, y + (self.item_height // 2), text=text, fill="#FFFFFF", anchor="w", font=("Yu Gothic UI", 12, "normal"))
