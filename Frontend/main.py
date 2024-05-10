import asyncio
import tkinter as tk
from tkinter import filedialog, messagebox, ttk
from tkinter.simpledialog import askstring

# Import the necessary components from your provided module
from TrackManager import TrackManager

class TrackManagerGUI:
    def __init__(self, root):
        self.root = root
        self.track_manager = TrackManager()
        self.item_to_object = {}
        self.setup_ui()

    def setup_widgets(self):
        # Frame for the directory selection
        self.frame = ttk.Frame(self.root)
        self.frame.pack(padx=10, pady=10)

        # Button to choose directory
        self.btn_select_dir = ttk.Button(self.frame, text="Select Folder", command=self.load_directory)
        self.btn_select_dir.pack(side=tk.LEFT)

        # Listbox to display files
        self.file_listbox = tk.Listbox(self.root, width=100, height=5)
        self.file_listbox.pack(padx=10, pady=10)

        # Scrollbar for the listbox
        self.scrollbar = ttk.Scrollbar(self.root, orient='vertical', command=self.file_listbox.yview)
        self.scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.file_listbox.config(yscrollcommand=self.scrollbar.set)

    def setup_table(self):
        # Setting up the Treeview widget for displaying metadata
        self.tree = ttk.Treeview(self.root, columns=(
            "title", 
            "original_title", 
            "artist", 
            "artist_sort", 
            "original_artist", 
            "original_artist_sort", 
            "album", 
            "original_album", 
            "album_artist", 
            "grouping", 
            "include", 
            "mbid", 
            "type", 
            "sort_name", 
            "joinphrase"
        ), show='headings')
        self.tree["displaycolumns"]=(
            'title', 
            'artist', 
            'album', 
            'include'
            )
        self.tree.pack(expand=True, fill=tk.BOTH, padx=10, pady=10)

        # Defining the columns
        self.tree.heading("title", text="Track Title")
        self.tree.heading("original_title", text="")
        self.tree.heading("artist", text="Artist")
        self.tree.heading("artist_sort", text="")
        self.tree.heading("original_artist", text="")
        self.tree.heading("original_artist_sort", text="")
        self.tree.heading("album", text="Album")
        self.tree.heading("original_album", text="")
        self.tree.heading("album_artist", text="")
        self.tree.heading("grouping", text="")
        self.tree.heading("include", text="Set")
        self.tree.heading("mbid", text="ID")
        self.tree.heading("type", text="Type")
        self.tree.heading("joinphrase", text="Join Phrase")

        # Column widths
        self.tree.column("title", width=100)
        self.tree.column("original_title", width=100)
        self.tree.column("artist", width=100)
        self.tree.column("artist_sort", width=100)
        self.tree.column("original_artist", width=100)
        self.tree.column("original_artist_sort", width=100)
        self.tree.column("album", width=100)
        self.tree.column("original_album", width=100)
        self.tree.column("album_artist", width=100)
        self.tree.column("grouping", width=100)
        self.tree.column("include", width=30)
        self.tree.column("mbid", width=100)
        self.tree.column("type", width=100)
        self.tree.column("joinphrase", width=100)

        self.tree.bind("<Button-1>", self.on_single_click)
        self.tree.bind("<Double-1>", self.on_double_click)

        # Button to update metadata
        self.update_button = ttk.Button(self.root, text="Save Changes", command=self.save_changes)
        self.update_button.pack(pady=10)


    def setup_ui(self):
        self.root.title("Track Manager")
        self.setup_widgets()
        self.setup_table()

    def load_directory(self):
        directory = filedialog.askdirectory()
        if directory:
            try:
                asyncio.run(self.track_manager.load_directory(directory))
                self.populate_table()
                messagebox.showinfo("Success", "Metadata saved successfully!")
            except Exception as e:
                messagebox.showerror("Error", str(e))

    def populate_table(self):
        for item in self.tree.get_children():
            self.tree.delete(item)
        self.item_to_object.clear()
        for track in self.track_manager.tracks:
            for artist_detail in track.mbArtistDetails:
                row = self.tree.insert("", "end", values=(
                    track.title, 
                    "original_title", 
                    artist_detail.name, 
                    artist_detail.sort_name, 
                    "original_artist", 
                    "original_artist_sort", 
                    track.album, 
                    "original_album", 
                    "album_artist", 
                    "grouping", 
                    '☐',
                    artist_detail.mbid, 
                    artist_detail.type, 
                    "sort_name", 
                    artist_detail.joinphrase
                ))


                self.tree.set(row, 'include', '☐')

                self.item_to_object[row] = {"track":track, "artist_detail":artist_detail}

    def save_changes(self):
        try:
            asyncio.run(self.track_manager.save_files())
            messagebox.showinfo("Success", "Metadata saved successfully!")
        except Exception as e:
            messagebox.showerror("Error", str(e))

    def on_single_click(self, event):
        region = self.tree.identify("region", event.x, event.y)
        if region == "cell":
            row = self.tree.identify_row(event.y)
            column = self.tree.identify_column(event.x)
            if self.tree.column(column)["id"] == "include":
                current_value = self.tree.set(row, 'include')
                self.tree.set(row, 'include', '☑' if current_value == '☐' else '☐')

    def on_double_click(self, event):
        # Get the treeview item clicked
        region = self.tree.identify("region", event.x, event.y)
        if region == "cell":
            row = self.tree.identify_row(event.y)
            column = self.tree.identify_column(event.x)
            if self.tree.column(column)["id"] == "sort_name":
                self.edit_cell(row, column, event)


    def edit_cell(self, row, column, event):
        # cell_x, cell_y, cell_width, cell_height = self.tree.bbox(row_id, column)
        # # Create entry widget to edit cell value, make it borderless and flat.
        # entry = ttk.Entry(self.root)
        # entry.place(x=cell_x, y=cell_y, width=cell_width, height=cell_height)
        # # entry.configure(font=self.tree["font"])  # Match font with the Treeview

        # # Set current value and select it
        # current_value = self.tree.set(row_id, column)
        # entry.insert(0, current_value)
        # entry.select_range(0, tk.END)
        # entry.focus()

        entry = ttk.Entry(self.root, width=10)
        entry.place(x=event.x, y=event.y)
        entry.insert(0, self.tree.set(row, column))
        entry.focus()

        # Function to save the new value
        def save_new_value(event):
            new_value = entry.get()
            self.tree.set(row, column=column, value=entry.get())
            entry.destroy()
            # Here you update the underlying data structure
            row_track = self.item_to_object.get(row)
            if row_track:
                row_track["artist_detail"].sort_name  = new_value
            
            self.populate_table()

            a = 22

        entry.bind("<Return>", save_new_value)
        entry.bind("<FocusOut>", save_new_value)


    def run_sync(self, async_func, *args, **kwargs):
        loop = asyncio.get_event_loop()
        loop.run_until_complete(async_func(*args, **kwargs))

def main():
    root = tk.Tk()
    app = TrackManagerGUI(root)
    root.mainloop()

if __name__ == "__main__":
    main()
