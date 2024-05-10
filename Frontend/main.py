import asyncio
import tkinter as tk
from tkinter import filedialog, messagebox, ttk
from tkinter.simpledialog import askstring

# Import the necessary components from your provided module
from TrackManager import TrackManager

class TrackManagerGUI:
    # Mapping between columns and source data in format
    data_mapping = {
        "file_path": {"source_object":"track_details", "property":"file_path", "display_name":"File Path", "width":100, "editable":False, "display":False},
        "title": {"source_object":"track_details", "property":"title", "display_name":"Track Title", "width":100, "editable":False, "display":True},
        "original_title": {"source_object":"track_details", "property":"original_title", "display_name":"Orig Title", "width":100, "editable":False, "display":False},
        "artist": {"source_object":"track_details", "property":"artist", "display_name":"Artist", "width":100, "editable":True, "display":True},
        "artist_sort": {"source_object":"mbartist_details", "property":"sort_name", "display_name":"Sort Artist", "width":100, "editable":False, "display":False},
        "original_artist": {"source_object":"track_details", "property":"original_artist", "display_name":"Orig Artist", "width":100, "editable":False, "display":False},
        "album": {"source_object":"track_details", "property":"album", "display_name":"Album", "width":100, "editable":False, "display":True},
        "original_album": {"source_object":"track_details", "property":"original_album", "display_name":"Orig Album", "width":100, "editable":False, "display":False},
        "album_artist": {"source_object":"track_details", "property":"album_artist", "display_name":"Album Artist", "width":100, "editable":False, "display":False},
        "grouping": {"source_object":"track_details", "property":"grouping", "display_name":"Grouping", "width":100, "editable":False, "display":False},
        "include": {"source_object":"mbartist_details", "property":"include", "display_name":"Set", "width":100, "editable":True, "display":True},
        "mbid": {"source_object":"mbartist_details", "property":"mbid", "display_name":"MBID", "width":100, "editable":False, "display":False},
        "type": {"source_object":"mbartist_details", "property":"type", "display_name":"Type", "width":100, "editable":False, "display":False},
        "joinphrase": {"source_object":"mbartist_details", "property":"joinphrase", "display_name":"Join Phrase", "width":100, "editable":False, "display":False},
        "custom_name": {"source_object":"mbartist_details", "property":"custom_name", "display_name":"Custom Name", "width":100, "editable":True, "display":True},
        "custom_original_name": {"source_object":"mbartist_details", "property":"custom_original_name", "display_name":"Custom Orig Name", "width":100, "editable":True, "display":True}
    }

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
        self.file_listbox = tk.Listbox(self.root)
        self.file_listbox.pack(expand=True, fill=tk.BOTH, padx=10, pady=10)

        # Scrollbar for the listbox
        self.scrollbar = ttk.Scrollbar(self.root, orient='vertical', command=self.file_listbox.yview)
        self.scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.file_listbox.config(yscrollcommand=self.scrollbar.set)

    def setup_table(self):
        # Initialize the Treeview widget
        self.tree = ttk.Treeview(self.root, columns=tuple(self.data_mapping.keys()), show='headings')
        self.tree.pack(expand=True, fill=tk.BOTH, padx=10, pady=10)

        display_columns = [column_id for column_id, settings in self.data_mapping.items() if settings.get("display", False)]
        self.tree["displaycolumns"] = display_columns

        # Set properties for each column
        for column_id, settings in self.data_mapping.items():
            self.tree.heading(column_id, text=settings["display_name"])
            self.tree.column(column_id, width=settings["width"])

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
                self.track_manager = TrackManager()
                asyncio.run(self.track_manager.load_directory(directory))
                self.populate_table()
            except Exception as e:
                messagebox.showerror("Error", str(e))

    def populate_table(self):
        # Clear existing items in the tree
        for item in self.tree.get_children():
            self.tree.delete(item)
        self.item_to_object.clear()

        # Populate the tree with new data
        for track in self.track_manager.tracks:
            for artist_detail in track.mbArtistDetails:
                values = []
                for column_id, settings in self.data_mapping.items():
                    # Determine the source object and property
                    if settings["source_object"] == "track_details":
                        value = getattr(track, settings["property"], "")
                    else:
                        value = getattr(artist_detail, settings["property"], "")

                    values.append(value)

                # Insert the new row into the treeview
                row = self.tree.insert("", "end", values=tuple(values))

                if "include" in self.data_mapping and self.data_mapping["include"]["source_object"] == "mbartist_details":
                    self.tree.set(row, 'include', '☑' if artist_detail.include == True else '☐')

                # Map the row to the corresponding objects for reference
                self.item_to_object[row] = {"track": track, "artist_detail": artist_detail}

    def save_changes(self):
        try:
            asyncio.run(self.track_manager.save_files())
            messagebox.showinfo("Success", "Metadata saved successfully!")
        except Exception as e:
            messagebox.showerror("Error", str(e))

    def get_clicked_cell(self, event):
        region = self.tree.identify("region", event.x, event.y)

        if(region != "cell"):
            return None
        
        return {
            "row": self.tree.identify_row(event.y),
            "column": self.tree.identify_column(event.x)
        }

    def on_single_click(self, event):
        clicked = self.get_clicked_cell(event)
        if (clicked == None):
            return
            
        # include is the only column that changes its value on a single click,
        # so it needs to be treated differently
        if ((self.tree.column(clicked["column"])["id"] == "include") and 
            (self.data_mapping[self.tree.column(clicked["column"])["id"]]["editable"] == True)):
            
            row_track = self.item_to_object.get(clicked["row"])
            if (None == row_track):
                raise Exception("Row has no track details.")
            
            current_value = self.tree.set(clicked["row"], clicked["column"])
            # clicking doesn't automatically change the value, so we need to flip it
            new_value = False if current_value == '☑' else True

            valueChanged = self.save_value_to_manager(new_value, self.tree.column(clicked["column"])["id"], row_track["track"], row_track["artist_detail"])
            if(valueChanged == True):
                self.populate_table()

    def on_double_click(self, event):
        clicked = self.get_clicked_cell(event)
        if(clicked == None):
            return

        if (self.data_mapping[self.tree.column(clicked["column"])["id"]]["editable"] == True):
            self.edit_cell(clicked["row"], clicked["column"], event)

    def edit_cell(self, row, column, event):
        entry = ttk.Entry(self.root, width=10)
        entry.place(x=event.x, y=event.y)
        entry.insert(0, self.tree.set(row, column))
        entry.focus()

        def save_new_value(event):
            new_value = entry.get()
            self.tree.set(row, column=column, value=entry.get())
            entry.destroy()

            # Update the underlying data structure
            row_track = self.item_to_object.get(row)
            if (None == row_track):
                raise Exception("Row has no track details.")
            
            valueChanged = self.save_value_to_manager(new_value, self.tree.column(column)["id"], row_track["track"], row_track["artist_detail"])
            
            if(valueChanged == True):
                self.populate_table()

        entry.bind("<Return>", save_new_value)
        entry.bind("<FocusOut>", save_new_value)

    def save_value_to_manager(self, new_value, column_id:str, track_details, mbartist_details) -> bool:
        if column_id not in self.data_mapping:
            raise Exception(f"column id {column_id} was not found in data mapping.")
        
        # Retrieve the object and attribute name
        mapping = self.data_mapping[column_id]
        if(mapping["source_object"] == "track_details"):
            source_obj = track_details
        else:
            source_obj = mbartist_details

        current_value = getattr(source_obj, mapping["property"])

        if new_value == current_value:
            return False

        # Update the value
        setattr(source_obj, mapping["property"], new_value)
        return True


    def run_sync(self, async_func, *args, **kwargs):
        loop = asyncio.get_event_loop()
        loop.run_until_complete(async_func(*args, **kwargs))

def main():
    root = tk.Tk()
    app = TrackManagerGUI(root)
    root.mainloop()

if __name__ == "__main__":
    main()
