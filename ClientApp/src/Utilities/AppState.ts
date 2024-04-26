import {action, observable, runInAction} from "mobx";
import {Error} from './DataTypes/Error';
import {User} from "./DataTypes/User";
import {Account} from "./DataTypes/Account";
import {IfcSettingsSet} from "./DataTypes/IfcSettingsSet";
import {Log} from "./DataTypes/Log";
import {Project} from "./DataTypes/Project";
import {ApiCalls} from "./ApiCalls";

export class AppState{
    @observable public errors: Error[] = [];
    @observable public accounts: Account[] = [];
    @observable public ifcSettings: IfcSettingsSet[] = [];
    @observable public logs: Log[] = [];
    @observable public projects: Project[] = [];
    @observable public authUrl?: string;
    @observable public user?: User;
    @observable public timezones: string[] = [];
    @observable public ready: boolean = false;

    public history: any;

    @action setErrors(errors: Error[]){
        this.errors = errors;
    }

    constructor() {
        new Promise((resolve, reject)=>{
            this.fetchUser()
                .then(()=>resolve())
                .catch(()=>{

                    reject();
                })
        })
            .then(()=> {
                return Promise.all([
                    this.fetchTimezones(),
                    this.fetchIfcSettings(),
                    this.fetchAccounts(),
                    this.fetchLogs(),
                    this.fetchProjects()
                ])
            })
            .finally(()=>{this.setReady(true)})
    }

    @action public setReady(state: boolean){
        this.ready = state;
    }

    @action public setAuthUrl(authUrl: string){
        this.authUrl = authUrl;
    }

    // public createWebhooks(){
    //     ApiCalls.postWebhooks();
    // }

    private fetchUser(){
        return new Promise((resolve, reject)=>{
            ApiCalls.fetchUser()
                .then((user)=>{
                    runInAction(()=>{
                        this.user = new User(user);
                        resolve();
                    })
                })
                .catch((err: unknown)=>{
                    ApiCalls.fetchAuthUrl()
                        .then((authUrl: string)=>{
                            this.setAuthUrl(authUrl);
                            reject();
                        })

                    console.error("ERROR", err)
                })
        })
    }

    private fetchTimezones(){
        return ApiCalls.getTimezones()
            .then((timezones)=>{
                runInAction(()=>{this.timezones = timezones})
            })
    }

    private fetchIfcSettings(){
        return ApiCalls.getIfcSettings()
            .then((ifcSettings)=>{
                runInAction(()=>{this.ifcSettings = ifcSettings
                    .map(val=>new IfcSettingsSet(val))
                    .sort((a,b)=>a.name.localeCompare(b.name, 'en', {numeric: true}))
                });
            })
    }

    private fetchAccounts(){
        return ApiCalls.GetApsAccounts()
            .then((accounts)=>{
                runInAction(()=>{this.accounts = accounts.map(account=>new Account(account))})
            })
    }

    private fetchProjects(){
        return ApiCalls.GetApsProjects()
            .then((projects)=>{
                runInAction(()=>{
                    this.projects = projects
                        .map(project=>new Project(project))
                        .sort((a,b)=>a.name.localeCompare(b.name, 'en', {numeric: true}))

                })
            })
    }

    private fetchLogs(){
        return ApiCalls.getLogs()
            .then((logNames)=>{
                runInAction(()=>{this.logs = logNames.map(name=>new Log({name}))})
            })
    }

    @action public addIfcSettingsSet(ifcSettings: IfcSettingsSet){
        this.ifcSettings = this.ifcSettings
            .concat([ifcSettings])
            .sort((a,b)=>a.name.localeCompare(b.name, 'en', {numeric: true}))
        ;
    }

    @action public removeIfcSettingsSet(ifcSettings: IfcSettingsSet){
        this.ifcSettings = this.ifcSettings.filter(val=>val !== ifcSettings);
    }
}