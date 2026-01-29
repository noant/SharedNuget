import os
import sys
import shutil

def rename_item(item_path):
    """Rename a single file or folder by adding _DONE_ prefix"""
    parent_dir = os.path.dirname(item_path)
    item_name = os.path.basename(item_path)
    
    if item_name.startswith("_DONE_"):
        return item_path, True
    
    new_item_name = f"_DONE_{item_name}"
    new_item_path = os.path.join(parent_dir, new_item_name)
    
    if os.path.exists(new_item_path):
        print(f"Warning: Target already exists, skipping: {new_item_path}")
        return item_path, False
    
    try:
        shutil.move(item_path, new_item_path)
        return new_item_path, True
    except Exception as e:
        print(f"Error renaming {item_path}: {e}")
        return item_path, False

def mark_feature_done(feature_path):
    if not os.path.exists(feature_path):
        print(f"Error: Path not found: {feature_path}")
        return False
    
    # If it's a file, just rename it
    if os.path.isfile(feature_path):
        new_path, success = rename_item(feature_path)
        if success:
            print(f"Successfully marked file as done:")
            print(f"  From: {feature_path}")
            print(f"  To:   {new_path}")
        return success
    
    # If it's a directory, rename all contents first, then the directory itself
    if not os.path.isdir(feature_path):
        print(f"Error: Path is neither file nor directory: {feature_path}")
        return False
    
    folder_name = os.path.basename(feature_path)
    folder_already_done = folder_name.startswith("_DONE_")
    
    if folder_already_done:
        print(f"Directory already marked as done: {folder_name}")
        print(f"Checking contents...")
    else:
        print(f"Processing directory: {feature_path}")
    
    # Rename all files and subdirectories inside
    items_to_rename = []
    for root, dirs, files in os.walk(feature_path, topdown=False):
        # Collect all files
        for file in files:
            file_path = os.path.join(root, file)
            items_to_rename.append(file_path)
        
        # Collect all subdirectories
        for dir_name in dirs:
            dir_path = os.path.join(root, dir_name)
            items_to_rename.append(dir_path)
    
    # Rename items
    renamed_count = 0
    for item_path in items_to_rename:
        _, success = rename_item(item_path)
        if success:
            renamed_count += 1
    
    if renamed_count > 0:
        print(f"Renamed {renamed_count} items inside the directory")
    else:
        print(f"All items inside already marked as done")
    
    # Finally, rename the directory itself (if not already done)
    if folder_already_done:
        print(f"Directory already has _DONE_ prefix, skipping directory rename")
        return True
    
    parent_dir = os.path.dirname(feature_path)
    new_folder_name = f"_DONE_{folder_name}"
    new_path = os.path.join(parent_dir, new_folder_name)
    
    if os.path.exists(new_path):
        print(f"Error: Target path already exists: {new_path}")
        return False
    
    try:
        shutil.move(feature_path, new_path)
        print(f"Successfully marked feature as done:")
        print(f"  From: {feature_path}")
        print(f"  To:   {new_path}")
        return True
    except Exception as e:
        print(f"Error renaming folder: {e}")
        return False

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python mark_feature_done.py <feature_folder_path>")
        print("Example: python mark_feature_done.py Spec/Features/002-dynamic-provider-refresh")
        sys.exit(1)
    
    feature_path = sys.argv[1]
    
    if not os.path.isabs(feature_path):
        feature_path = os.path.abspath(feature_path)
    
    success = mark_feature_done(feature_path)
    sys.exit(0 if success else 1)
