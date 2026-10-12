import tkinter as tk
import customtkinter as ctk
from i18n import t

class WaypointListCanvas(ctk.CTkFrame):
    """経由地の一覧。実行順（上から順）に番号を付けて表示し、ドラッグで並び替えられる。
    挿入位置は2本線で示す（右側のアクションリストと同じ操作）"""
    def __init__(self, master, points, visible_height=96):
        super().__init__(master, fg_color="#202020")
        self.points = points          # 呼び出し元と共有する [[x, y], ...]
        self.item_height = 22
        self.visible_height = visible_height
        self.top = 0                  # スクロール位置（px）
        self.drag_index = None
        self.insert_index = None

        self.canvas = tk.Canvas(self, bg="#202020", bd=0, highlightthickness=0, height=visible_height)
        self.canvas.pack(fill="x", expand=True, padx=4, pady=4)
        self.canvas.bind("<Button-1>", self.on_press)
        self.canvas.bind("<B1-Motion>", self.on_motion)
        self.canvas.bind("<ButtonRelease-1>", self.on_release)
        self.canvas.bind("<MouseWheel>", self.on_wheel)
        self.canvas.bind("<Configure>", lambda event: self.draw())
        self.draw()

    def content_height(self):
        return len(self.points) * self.item_height + self.item_height // 2

    def clamp_top(self):
        self.top = max(0, min(self.top, self.content_height() - self.visible_height))

    def on_press(self, event):
        index = (event.y + self.top) // self.item_height
        if 0 <= index < len(self.points):
            self.drag_index = index
            self.draw(dragging_y=event.y + self.top)

    def on_motion(self, event):
        if self.drag_index is None: return
        y = event.y + self.top
        raw_index = (y + self.item_height // 2) // self.item_height
        self.insert_index = max(0, min(raw_index, len(self.points)))
        self.draw(dragging_y=y)

    def on_release(self, event):
        if self.drag_index is None: return
        if self.insert_index is not None:
            target = self.insert_index
            if self.drag_index < target: target -= 1
            if target != self.drag_index and 0 <= target < len(self.points):
                item = self.points.pop(self.drag_index)
                self.points.insert(target, item)
        self.drag_index = None
        self.insert_index = None
        self.draw()

    def on_wheel(self, event):
        self.top -= (event.delta // 120) * self.item_height
        self.clamp_top()
        self.draw()

    def draw(self, dragging_y=None):
        self.canvas.delete("all")
        width = self.canvas.winfo_width()
        if width <= 1: width = 260
        if not self.points:
            self.top = 0
            self.canvas.create_text(10, self.item_height // 2 + 2, text=t("no_waypoints"), fill="gray", anchor="w", font=("Yu Gothic UI", 11, "normal"))
            return

        y = 0
        for i, point in enumerate(self.points):
            if i == self.drag_index:
                continue
            if i == self.insert_index:
                self.draw_insertion_lines(y - self.top, width)
                y += self.item_height // 2
            self.draw_item(i, point, y - self.top, width)
            y += self.item_height

        if self.insert_index is not None and self.insert_index == len(self.points):
            self.draw_insertion_lines(y - self.top, width)

        if self.drag_index is not None and dragging_y is not None:
            self.draw_item(self.drag_index, self.points[self.drag_index], dragging_y - self.item_height // 2 - self.top, width, dragging=True)

    def draw_insertion_lines(self, y, width):
        accent = "#1F6AA5"
        space = 4
        self.canvas.create_line(6, y - space, width - 6, y - space, fill=accent, width=2)
        self.canvas.create_line(6, y + space, width - 6, y + space, fill=accent, width=2)

    def draw_item(self, index, point, y, width, dragging=False):
        bg = "#3A3A3A" if dragging else "#2B2B2B"
        outline = "#1F6AA5" if dragging else "#3E3E3E"
        self.canvas.create_rectangle(4, y + 2, width - 4, y + self.item_height - 2, fill=bg, outline=outline)
        self.canvas.create_text(12, y + self.item_height // 2, text=f"{index + 1}. ({point[0]}, {point[1]})", fill="#FFFFFF", anchor="w", font=("Yu Gothic UI", 11, "normal"))
