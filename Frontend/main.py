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
        self.tree = ttk.Treeview(self.root, columns=("Title", "Artist", "Album", "MBID", "Type", "Sort Name", "Join Phrase"), show='headings')
        self.tree.pack(expand=True, fill=tk.BOTH, padx=10, pady=10)

        # Defining the columns
        self.tree.heading("Title", text="Track Title")
        self.tree.heading("Artist", text="Artist")
        self.tree.heading("Album", text="Album")
        self.tree.heading("MBID", text="ID")
        self.tree.heading("Type", text="Type")
        self.tree.heading("Sort Name", text="Sort Name")
        self.tree.heading("Join Phrase", text="Join Phrase")

        # Column widths
        self.tree.column("Title", width=200)
        self.tree.column("Artist", width=150)
        self.tree.column("Album", width=150)
        self.tree.column("MBID", width=100)
        self.tree.column("Type", width=100)
        self.tree.column("Sort Name", width=100)
        self.tree.column("Join Phrase", width=100)
        self.tree.column("Type", width=100)

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
                row_id = self.tree.insert("", "end", values=(artist_detail.include, track.title, artist_detail.name, track.album, artist_detail.mbid, artist_detail.type, artist_detail.sort_name, artist_detail.joinphrase))
                self.item_to_object[row_id] = {"track":track, "artist_detail":artist_detail}

    def save_changes(self):
        try:
            asyncio.run(self.track_manager.save_files())
            messagebox.showinfo("Success", "Metadata saved successfully!")
        except Exception as e:
            messagebox.showerror("Error", str(e))

    def on_double_click(self, event):
        # Get the treeview item clicked
        region = self.tree.identify("region", event.x, event.y)
        if region == "cell":
            row_id = self.tree.identify_row(event.y)
            column = self.tree.identify_column(event.x)
            if self.tree.heading(column)['text'] == "Sort Name":
                self.edit_cell(row_id, column)

    def edit_cell(self, row_id, column):
        # Create entry widget to edit cell value
        entry = ttk.Entry(self.root)
        entry.place(x=self.tree.bbox(row_id, column)[0], y=self.tree.bbox(row_id, column)[1], width=self.tree.bbox(row_id, column)[2], height=self.tree.bbox(row_id, column)[3])

        # Set current value
        current_value = self.tree.set(row_id, column)
        entry.insert(0, current_value)
        entry.focus()

        # Function to save the new value
        def save_new_value(event):
            new_value = entry.get()
            self.tree.set(row_id, column, new_value)
            entry.destroy()
            # Here you update the underlying data structure
            row_track = self.item_to_object.get(row_id)
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
