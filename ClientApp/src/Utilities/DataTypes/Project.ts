import {Folder} from "./Folder";
import {ApiCalls} from "../ApiCalls";
import {action, computed, observable, runInAction} from "mobx";
import {Schedule} from "./Schedule";
import {TreeBase} from "./TreeBase";
import {appState} from "../../App";
import {File} from "./File";
import {ConversionJob} from "./ConversionJob";

export interface IProject {
    hubId: string;
    id: string;
    name: string;
}

export class Project{
    @computed public get id(){ return this.projectId}
    public readonly hubId: string;
    @computed public get accountId(){ return this.hubId.substring(2)}
    public readonly projectId: string;
    public readonly name: string;
    @observable public fetched: boolean = false;

    @observable public checked: string[] = [];
    @observable public expanded: string[] = [];

    @observable public folders: Folder[] = [];
    @computed public get files(): File[] {return Object.values(this.filesHash)};
    @observable public schedules: Schedule[] = [];
    @observable public conversionJobs: ConversionJob[] = [];
    public limit: number = 50;
    @observable public offset: number = 0;

    public foldersHash: {[key: string]: Folder} = {};
    public filesHash: {[key: string]: File} = {};

    constructor({hubId, id, name}: IProject) {
        this.hubId = hubId;
        this.projectId = id;
        this.name = name;
    }

    @action.bound setChecked(checked: string[]){console.log("CHECKED", checked); this.checked = checked;}
    @action.bound setExpanded(expanded: string[]){ this.expanded = expanded;}

    @computed public get fullyLoaded(){return (this.conversionJobs.length + this.limit) <= this.offset}

    public getAllIds(){
        return this.getFlatChildrenArray()
            .map(val=>val.id)
    }

    public getFlatChildrenArray(){
        return Object.values(this.foldersHash)
            .reduce((acc: TreeBase[], folder: Folder)=>{
                return acc
                    .concat([folder])
                    .concat(folder.children.filter(val=>!(val instanceof Folder)))
            }, [])
    }

    public getSchedules(){
        ApiCalls.GetProjectSchedule({project: this})
            .then(schedules=>{
                runInAction(()=>this.schedules = schedules.map(val=>new Schedule(val, this)))
            })
    }

    @action.bound public getConversionJobs(reset?: boolean){
        if(reset){
            this.offset = 0;
            this.conversionJobs = [];
        }

        ApiCalls.GetConversionJobs({project: this})
            .then(conversionJobs=>{
                runInAction(()=>{
                    this.conversionJobs = this.conversionJobs.concat(conversionJobs.map(val=>new ConversionJob(val, this)))
                    this.offset += this.limit;
                })
            })
    }

    @action public removeSchedule(schedule: Schedule){
        this.schedules = this.schedules.filter(val=> val !== schedule);
    }

    public getFolders(){
        this.getSchedules();

        ApiCalls.GetApsTopFolders({project: this})
            .then((folders)=> {
                const folderObjects = folders
                    .map(val=>new Folder({...val, project: this}))
                    .sort((a,b)=>a.name.localeCompare(b.name, 'en', {numeric: true}));
                folderObjects.forEach(folder=>{this.foldersHash[folder.id] = folder});
                runInAction(()=>{
                    this.folders = folderObjects;
                    this.fetched = true;
                });
            })
            .catch(err=>console.error(err));
    }

    public expandAllFolders(): Promise<any>{
        return new Promise(async (resolve)=>{
            while(Object.values(this.foldersHash).filter(val=>!val.fetched).length){
                const folders = Object.values(this.foldersHash).filter(val=>!val.fetched).slice(0,5);
                await Promise.all(folders.map(val=>val.fetch()))
            }
            resolve();
        })
    }

    public createSchedule(name: string){
        const flatChildrenArray = this.getFlatChildrenArray();

        const fileUrns = flatChildrenArray
            .filter(val=> val.isChecked() && val.id.indexOf('urn:adsk.wipprod:fs.file:') > -1)
            .map(val=>val.id)
        const folderUrns = flatChildrenArray
            .filter(val=> val.isChecked() && val instanceof Folder && !(val.parent?.isChecked()))
            .map(val=>val.id)

        const files = this.files
            .filter(val=>fileUrns.indexOf(val.id) > -1)
            .map(val=>val.toInterface())

        const newSchedule = new Schedule({
            cron: "0 0 * * *",
            timeZoneId: appState.timezones[0],
            files: files,
            folderUrns: folderUrns,
            lastFileCount: fileUrns.length,
            ifcSettingsName: appState.ifcSettings.find(val=>val.isDefault)?.name || appState.ifcSettings[0]?.name || "",
            lastStart: "",
            name: name
        }, this);

        ApiCalls.PostProjectSchedule({schedule: newSchedule})
            .then((result)=>{
                runInAction(()=>{
                    this.schedules = this.schedules.concat([new Schedule(result, this)])
                })
                appState.history.push(`/projects/${this.id}/schedules/${result.id}`)
            })
    }

    @computed public get searchTerm(){return `${this.name}`.toLowerCase()}
}