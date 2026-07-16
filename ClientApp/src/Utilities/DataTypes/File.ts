import {ITreeBase, TreeBase} from "./TreeBase";
import {makeObservable} from "mobx";

export interface IFile extends ITreeBase {
    showCheckbox?: boolean;
    itemId: string;
    folderId?: string;
    isCompositeDesign: boolean;
}

export interface IFileToUpload{
    id: string;
    itemId: string;
    name: string;
    folderId: string;
    isCompositeDesign: boolean;
}

export class File extends TreeBase{
    public readonly showCheckbox: boolean = true;
    public readonly itemId: string;
    public readonly folderId: string;
    public readonly isCompositeDesign: boolean;

    constructor(props: IFile) {
        super(props);
        makeObservable(this);
        this.itemId = props.itemId;
        this.folderId = props.folderId || "";
        this.isCompositeDesign = props.isCompositeDesign || false;
        this.showCheckbox = props.showCheckbox === undefined ? true : props.showCheckbox;
    }
    public getTree(){
        return {
            label: this.newLabel,
            value: this.value,
            showCheckbox: this.showCheckbox,
            icon: !this.showCheckbox ? "" : null,
            className: !this.showCheckbox ? 'hiddenRow' : null
        }
    }

    public toInterface(): IFileToUpload{
        return {
            id: this.id,
            itemId: this.itemId,
            name: this.name,
            folderId: this.folderId,
            isCompositeDesign: this.isCompositeDesign
        }
    }

    public get newLabel(): string {
        console.log("IS COMPOSITE")
        return this.isCompositeDesign ? `${this.name} (zip)` : this.name;
    }
}