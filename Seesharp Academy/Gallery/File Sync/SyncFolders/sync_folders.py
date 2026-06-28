"""
SyncFolders - 文件夹同步工具
比较两个目录，列出A中有但B中没有的文件，支持选择性复制。
"""
import os
import shutil
import threading
import tkinter as tk
from tkinter import ttk, filedialog, messagebox
from pathlib import Path


class SyncFoldersApp:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("SyncFolders - 文件夹同步工具")
        self.root.geometry("1000x700")
        self.root.minsize(800, 500)

        self.diff_files: list[str] = []  # 相对路径列表

        self._build_ui()

    # ── UI ──────────────────────────────────────────────
    def _build_ui(self):
        # --- 顶部：目录输入 ---
        top = ttk.Frame(self.root, padding=8)
        top.pack(fill=tk.X)

        ttk.Label(top, text="目录 A (源):").grid(row=0, column=0, sticky=tk.W)
        self.var_dir_a = tk.StringVar()
        ttk.Entry(top, textvariable=self.var_dir_a, width=60).grid(row=0, column=1, padx=4, sticky=tk.EW)
        ttk.Button(top, text="浏览…", command=lambda: self._browse(self.var_dir_a)).grid(row=0, column=2, padx=2)

        ttk.Label(top, text="目录 B (目标):").grid(row=1, column=0, sticky=tk.W, pady=(4, 0))
        self.var_dir_b = tk.StringVar()
        ttk.Entry(top, textvariable=self.var_dir_b, width=60).grid(row=1, column=1, padx=4, sticky=tk.EW, pady=(4, 0))
        ttk.Button(top, text="浏览…", command=lambda: self._browse(self.var_dir_b)).grid(row=1, column=2, padx=2, pady=(4, 0))

        top.columnconfigure(1, weight=1)

        # --- 操作按钮 ---
        btn_frame = ttk.Frame(self.root, padding=(8, 0))
        btn_frame.pack(fill=tk.X, pady=4)

        self.btn_compare = ttk.Button(btn_frame, text="🔍 比较", command=self._on_compare)
        self.btn_compare.pack(side=tk.LEFT, padx=2)

        self.btn_copy_sel = ttk.Button(btn_frame, text="📋 复制选中项到 B", command=self._on_copy_selected, state=tk.DISABLED)
        self.btn_copy_sel.pack(side=tk.LEFT, padx=2)

        self.btn_select_all = ttk.Button(btn_frame, text="全选", command=self._select_all, state=tk.DISABLED)
        self.btn_select_all.pack(side=tk.LEFT, padx=2)

        self.btn_deselect_all = ttk.Button(btn_frame, text="取消全选", command=self._deselect_all, state=tk.DISABLED)
        self.btn_deselect_all.pack(side=tk.LEFT, padx=2)

        self.lbl_status = ttk.Label(btn_frame, text="就绪", foreground="gray")
        self.lbl_status.pack(side=tk.RIGHT)

        # --- 进度条 ---
        self.progress = ttk.Progressbar(self.root, mode="determinate")
        self.progress.pack(fill=tk.X, padx=8, pady=(0, 4))

        # --- 中间：Treeview 文件列表 ---
        tree_frame = ttk.Frame(self.root, padding=8)
        tree_frame.pack(fill=tk.BOTH, expand=True)

        columns = ("rel_path", "size", "status")
        self.tree = ttk.Treeview(tree_frame, columns=columns, show="headings", selectmode="extended")
        self.tree.heading("rel_path", text="文件相对路径")
        self.tree.heading("size", text="大小")
        self.tree.heading("status", text="状态")

        self.tree.column("rel_path", width=600, minwidth=200)
        self.tree.column("size", width=100, minwidth=80, anchor=tk.E)
        self.tree.column("status", width=120, minwidth=80, anchor=tk.CENTER)

        scrollbar_y = ttk.Scrollbar(tree_frame, orient=tk.VERTICAL, command=self.tree.yview)
        scrollbar_x = ttk.Scrollbar(tree_frame, orient=tk.HORIZONTAL, command=self.tree.xview)
        self.tree.configure(yscrollcommand=scrollbar_y.set, xscrollcommand=scrollbar_x.set)

        self.tree.grid(row=0, column=0, sticky=tk.NSEW)
        scrollbar_y.grid(row=0, column=1, sticky=tk.NS)
        scrollbar_x.grid(row=1, column=0, sticky=tk.EW)
        tree_frame.rowconfigure(0, weight=1)
        tree_frame.columnconfigure(0, weight=1)

        # 右键菜单
        self.ctx_menu = tk.Menu(self.tree, tearoff=0)
        self.ctx_menu.add_command(label="复制选中项到 B", command=self._on_copy_selected)
        self.tree.bind("<Button-3>", self._show_ctx_menu)

        # --- 底部日志 ---
        log_frame = ttk.LabelFrame(self.root, text="日志", padding=4)
        log_frame.pack(fill=tk.X, padx=8, pady=(0, 8))

        self.txt_log = tk.Text(log_frame, height=6, state=tk.DISABLED, wrap=tk.WORD, font=("Consolas", 9))
        sb_log = ttk.Scrollbar(log_frame, orient=tk.VERTICAL, command=self.txt_log.yview)
        self.txt_log.configure(yscrollcommand=sb_log.set)
        self.txt_log.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        sb_log.pack(side=tk.RIGHT, fill=tk.Y)

    # ── 辅助 ────────────────────────────────────────────
    def _browse(self, var: tk.StringVar):
        d = filedialog.askdirectory()
        if d:
            var.set(d)

    def _log(self, msg: str):
        self.txt_log.configure(state=tk.NORMAL)
        self.txt_log.insert(tk.END, msg + "\n")
        self.txt_log.see(tk.END)
        self.txt_log.configure(state=tk.DISABLED)

    def _set_status(self, msg: str):
        self.lbl_status.configure(text=msg)
        self.root.update_idletasks()

    def _set_busy(self, busy: bool):
        state = tk.DISABLED if busy else tk.NORMAL
        self.btn_compare.configure(state=state)

    def _show_ctx_menu(self, event):
        if self.tree.selection():
            self.ctx_menu.post(event.x_root, event.y_root)

    @staticmethod
    def _fmt_size(n: int) -> str:
        for unit in ("B", "KB", "MB", "GB"):
            if n < 1024:
                return f"{n:.1f} {unit}" if unit != "B" else f"{n} B"
            n /= 1024
        return f"{n:.1f} TB"

    def _select_all(self):
        self.tree.selection_set(self.tree.get_children())

    def _deselect_all(self):
        self.tree.selection_clear()

    # ── 扫描文件 ────────────────────────────────────────
    @staticmethod
    def _scan_dir(base: str) -> set[str]:
        """返回 base 下所有文件的相对路径集合（使用 / 分隔符）"""
        result = set()
        base_path = Path(base)
        for f in base_path.rglob("*"):
            if f.is_file():
                result.add(f.relative_to(base_path).as_posix())
        return result

    # ── 比较 ────────────────────────────────────────────
    def _on_compare(self):
        dir_a = self.var_dir_a.get().strip()
        dir_b = self.var_dir_b.get().strip()

        if not dir_a or not dir_b:
            messagebox.showwarning("提示", "请先输入两个目录路径。")
            return
        if not os.path.isdir(dir_a):
            messagebox.showerror("错误", f"目录 A 不存在:\n{dir_a}")
            return
        if not os.path.isdir(dir_b):
            messagebox.showerror("错误", f"目录 B 不存在:\n{dir_b}")
            return

        self._set_busy(True)
        self._set_status("正在扫描…")
        self.tree.delete(*self.tree.get_children())
        self.diff_files.clear()

        def worker():
            files_a = self._scan_dir(dir_a)
            files_b = self._scan_dir(dir_b)
            only_in_a = sorted(files_a - files_b)

            self.root.after(0, lambda: self._fill_tree(dir_a, only_in_a))

        threading.Thread(target=worker, daemon=True).start()

    def _fill_tree(self, dir_a: str, only_in_a: list[str]):
        self.diff_files = only_in_a
        self.progress["maximum"] = len(only_in_a)
        self.progress["value"] = 0

        base = Path(dir_a)
        # 按目录分组插入
        dir_groups: dict[str, list[str]] = {}
        for rel in only_in_a:
            parent = str(Path(rel).parent)
            dir_groups.setdefault(parent, []).append(rel)

        idx = 0
        for folder in sorted(dir_groups.keys()):
            # 文件夹节点
            folder_id = self.tree.insert("", tk.END, text=f"📁 {folder}", values=(folder, "", f"{len(dir_groups[folder])} 个文件"), open=False)
            for rel in dir_groups[folder]:
                full = base / rel
                size = full.stat().st_size if full.exists() else 0
                self.tree.insert(folder_id, tk.END, iid=rel, values=(rel, self._fmt_size(size), "仅A有"))
                idx += 1
                self.progress["value"] = idx
                self.root.update_idletasks()

        count = len(only_in_a)
        self._set_status(f"比较完成：A 中独有文件 {count} 个")
        self._log(f"比较完成 - A独有: {count} 个文件")

        has_items = count > 0
        self.btn_copy_sel.configure(state=tk.NORMAL if has_items else tk.DISABLED)
        self.btn_select_all.configure(state=tk.NORMAL if has_items else tk.DISABLED)
        self.btn_deselect_all.configure(state=tk.NORMAL if has_items else tk.DISABLED)
        self._set_busy(False)

    # ── 复制 ────────────────────────────────────────────
    def _on_copy_selected(self):
        dir_b = self.var_dir_b.get().strip()
        dir_a = self.var_dir_a.get().strip()

        # 收集选中的叶子节点（跳过文件夹节点）
        selected = []
        for iid in self.tree.selection():
            vals = self.tree.item(iid, "values")
            if vals and vals[2] == "仅A有":
                selected.append(vals[0])

        # 如果选中的是文件夹节点，把其子文件也加入
        for iid in self.tree.selection():
            children = self.tree.get_children(iid)
            for child in children:
                vals = self.tree.item(child, "values")
                if vals and vals[2] == "仅A有" and vals[0] not in selected:
                    selected.append(vals[0])

        if not selected:
            messagebox.showinfo("提示", "请先选择要复制的文件。\n可以按住 Ctrl/Shift 多选，或点击文件夹节点选中整组。")
            return

        ans = messagebox.askyesno("确认", f"将 {len(selected)} 个文件从 A 复制到 B，继续？")
        if not ans:
            return

        self._set_busy(True)
        self._set_status("正在复制…")
        self.progress["maximum"] = len(selected)
        self.progress["value"] = 0

        def worker():
            ok = 0
            fail = 0
            base_a = Path(dir_a)
            base_b = Path(dir_b)
            for i, rel in enumerate(selected):
                src = base_a / rel
                dst = base_b / rel
                try:
                    dst.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(str(src), str(dst))
                    ok += 1
                    self.root.after(0, lambda r=rel: self._mark_copied(r))
                except Exception as e:
                    fail += 1
                    self.root.after(0, lambda r=rel, err=e: self._log(f"[失败] {r}: {err}"))
                self.root.after(0, lambda v=i + 1: self.progress.configure(value=v))

            self.root.after(0, lambda: self._copy_done(ok, fail))

        threading.Thread(target=worker, daemon=True).start()

    def _mark_copied(self, rel: str):
        try:
            self.tree.item(rel, values=(rel, self.tree.item(rel, "values")[1], "✅ 已复制"))
        except tk.TclError:
            pass

    def _copy_done(self, ok: int, fail: int):
        self._set_status(f"复制完成：成功 {ok}，失败 {fail}")
        self._log(f"复制完成 - 成功: {ok}, 失败: {fail}")
        self._set_busy(False)
        messagebox.showinfo("完成", f"复制完成\n成功: {ok}\n失败: {fail}")


def main():
    root = tk.Tk()

    # 设置 DPI 感知（Windows 高分屏）
    try:
        from ctypes import windll
        windll.shcore.SetProcessDpiAwareness(1)
    except Exception:
        pass

    style = ttk.Style()
    try:
        style.theme_use("vista")
    except tk.TclError:
        try:
            style.theme_use("clam")
        except tk.TclError:
            pass

    # 让 Treeview 行高更舒适
    style.configure("Treeview", rowheight=22)

    SyncFoldersApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
