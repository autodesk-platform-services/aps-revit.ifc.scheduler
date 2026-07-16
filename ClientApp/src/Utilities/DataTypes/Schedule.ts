import {Project} from "./Project";
import {action, computed, observable} from "mobx";
import {Cron} from "./Cron";
import {ApiCalls} from "../ApiCalls";
import {Folder} from "./Folder";
import {appState} from "../../App";
import {IFileToUpload} from "./File";

export interface ISchedule {
    id?: string;
    cron: string;
    timeZoneId: string;
    name: string;
    ifcSettingsName: string;
    folderUrns: string[];
    files: IFileToUpload[];
    lastStart: string;
    lastFileCount: number;
    projectId?: string;
}

export class Schedule{
    public id?: string;
    @observable public cron: Cron;
    @observable public timeZoneId: string;
    @observable public name: string;
    @observable public ifcSettingsName: string;
    @observable public folderUrns: string[];
    @observable public fileUrns: string[];
    @observable public lastStart: string;
    public lastFileCount: number;
    public project: Project;

    @observable public checked: string[] = [];

    @action public setIfcSettingsName(name: string){this.ifcSettingsName = name;}
    @action public setName(name: string){this.name = name;}

    @observable private serializedInterface: string;

    constructor({id, cron, timeZoneId, name, ifcSettingsName, folderUrns, files, lastStart, lastFileCount}: ISchedule, project: Project) {
        this.id = id;
        this.cron = new Cron(cron);
        this.name = name;
        this.timeZoneId = timeZoneId;
        this.ifcSettingsName = ifcSettingsName;
        this.folderUrns = folderUrns;
        this.fileUrns = files.map(val=>val.id);
        this.lastStart = lastStart;
        this.lastFileCount = lastFileCount;
        this.project = project;
        this.checked = this.fileUrns.concat(this.folderUrns).concat(this.folderUrns.map(val=>`${val}_placeholderChild`))

        this.serializedInterface = this.internalInterface.slice();

        this.saveChanges = this.saveChanges.bind(this);
        this.delete = this.delete.bind(this);
    }
    @computed public get searchTerm(){return `${this.name}`.toLowerCase()}
    @action.bound setChecked(checked: string[]){
        this.checked = checked;

        const flatChildrenArray = this.project.getFlatChildrenArray();

        this.fileUrns = flatChildrenArray
            .filter(val=> val.isChecked(checked) && val.id.indexOf('urn:adsk.wipprod:fs.file:') > -1)
            .map(val=>val.id)
        this.folderUrns = flatChildrenArray
            .filter(val=> val.isChecked(checked) && val instanceof Folder && !(val.parent?.isChecked(checked)))
            .map(val=>val.id)
    }

    @action.bound setTimeZoneId(timeZoneId: string){
        this.timeZoneId = timeZoneId;
        console.log("Setting TimeZOneId", timeZoneId)
    }

    @computed public get hasChanged(){return this.serializedInterface !== this.internalInterface.slice()}

    @computed public get internalInterface(){
        return JSON.stringify({
            name: this.name,
            cron: this.cron.toString(),
            timeZoneId: this.timeZoneId,
            ifcSettingsName: this.ifcSettingsName,
            folderUrns: this.folderUrns,
            fileUrns: this.fileUrns,
            lastFileCount: this.lastFileCount,
            projectId: this.project.id
        });
    }

    public toInterface(){
        const files = this.project.files
            .filter(val=>this.fileUrns.indexOf(val.id) > -1)
            .map(val=>val.toInterface())

        return {
            name: this.name,
            cron: this.cron.toString(),
            timeZoneId: this.timeZoneId,
            ifcSettingsName: this.ifcSettingsName,
            folderUrns: this.folderUrns,
            files: files,
            lastFileCount: this.lastFileCount,
            projectId: this.project.id
        }
    }

    public saveChanges(){
            this.serializedInterface = this.internalInterface.slice();
            return ApiCalls.PatchProjectSchedule({schedule: this});
    }

    public delete(){
        return ApiCalls.DeleteProjectSchedule({schedule: this})
            .then(()=>{
                this.project.removeSchedule(this);
                appState.navigate(`/projects/${this.project.id}`)
            })
    }
}