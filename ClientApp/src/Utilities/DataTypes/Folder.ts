import {ITreeBase, TreeBase} from "./TreeBase";
import {action, computed, observable, runInAction} from "mobx";
import {ApiCalls} from "../ApiCalls";
import {File} from "./File";

export interface IFolder extends ITreeBase {

}

export class Folder extends TreeBase{
    @observable private _children: TreeBase[] = [];
    @observable public fetched: boolean = false;

    // constructor(props: IFolder) {
    //     super(props);
    // }

    @computed public get children(): TreeBase[]{
        //All children of folders must have unique Ids to use react-checkbox-tree
        if(!this.fetched){
            return [
                new File({
                    id: this.id + "_placeholderChild",
                    name: "-- Loading --",
                    itemId: "",
                    project: this.project,
                    showCheckbox: false,
                    isCompositeDesign: false
                })]
        } else if(!this._children.length){
            return [
                new File({
                    id: this.id + "_placeholderChild",
                    name: "-- No Contents --",
                    itemId: "",
                    project: this.project,
                    showCheckbox: false,
                    isCompositeDesign: false
                })]
        }
        return this._children
    }

    public getChildIds(): string[]{
        return this.children
            .map(val=>val.id)
            .concat(
                this.children
                    .reduce((acc: string[], val)=>{
                        return val instanceof Folder ? acc.concat(val.getChildIds()) : acc;
                    }, []))
    }

    public getTree(){
        return {
            label: this.label,
            value: this.value,
            children: this.children.map(val=>val.getTree())
        }
    }

    @action.bound public fetch(): Promise<TreeBase[]>{
        return new Promise((resolve, reject)=>{
            ApiCalls.GetApsFolder({folder: this})
                .then(results=>{
                    runInAction(()=>{
                        this._children = results.map(val=>{
                            console.log(val)
                            if(val.type === "folder"){
                                const folder = new Folder({...val, project: this.project, parent: this});
                                this.project.foldersHash[folder.id] = folder;
                                return folder;
                            } else {
                                //@ts-ignore
                                const file = new File({...val, itemId: val.itemId, project: this.project, parent: this});
                                this.project.filesHash[file.id] = file;
                                return file;
                            }
                        });
                        resolve(this._children)
                    })
                })
                .finally(()=>runInAction(()=>this.fetched = true))
        });
    }
}