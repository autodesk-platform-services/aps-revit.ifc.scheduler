import {appState} from '../App';
import axios from 'axios';
import {IfcSettingsSet, IIfcSettingsSet} from "./DataTypes/IfcSettingsSet";
import {Log} from "./DataTypes/Log";
import {IUser} from "./DataTypes/User";
import {IProject, Project} from "./DataTypes/Project";
import {ITreeBase} from "./DataTypes/TreeBase";
import {Folder} from "./DataTypes/Folder";
import {ISchedule, Schedule} from "./DataTypes/Schedule";
import {Error} from "./DataTypes/Error";
import {Account, IAccount} from "./DataTypes/Account";
import {IFile, IFileToUpload} from "./DataTypes/File";
import {IConversionJob} from "./DataTypes/ConversionJob";

export class ApiCalls{
    //Auth Calls
    public static fetchAuthUrl(){
        return SimpleGet<string>(`/api/aps/oauth/url?encodedRedirectUrl=${btoa(window.location.href)}`);
    }
    public static fetchUser(){
        return SimpleGet<IUser>(`/api/auth/user`);
    }
    public static logout(){
        return SimpleGet<void>(`/api/auth/logout`);
    }
    //IFC Settings
    public static getIfcSettings(){
        return SimpleGet<IIfcSettingsSet[]>(`/api/ifcsettings`);
    }
    public static postIfcSettings({ifcSettings}: {ifcSettings: IfcSettingsSet}){
        return SimplePost<IIfcSettingsSet>(`/api/ifcsettings`, ifcSettings);
    }
    public static patchIfcSettings({ifcSettings}: {ifcSettings: IfcSettingsSet}){
        return SimplePatch<IfcSettingsSet>(`/api/ifcsettings/${ifcSettings.id}`, ifcSettings);
    }
    public static deleteIfcSettings({ifcSettings}: {ifcSettings: IfcSettingsSet}){
        return SimpleDelete<void>(`/api/ifcsettings/${ifcSettings.id}`);
    }
    //Logs and Debugging
    public static getLogs(){
        return SimpleGet<string[]>(`/api/logs`);
    }
    public static getLogFile({log}: {log: Log}){
        return SimpleGet<string>(`/api/logs/${log.name}`);
    }
    //Webhooks
    // public static postWebhooks(){
    //     return SimplePost<string>(`/api/aps/webhooks/create`, {});
    // }

    //Timezones
    public static getTimezones(){
        return SimpleGet<string[]>(`/api/timezones`);
    }
    //Accounts
    public static PostAccount({account}: {account: Account}){
        return SimplePost<IAccount>(`/api/accounts`, account);
    }
    public static DeleteAccount({account}: {account: Account}){
        return SimpleDelete<void>(`/api/accounts/${account.id}`);
    }
    //APS
    public static GetApsAccounts(){
        return SimpleGet<IAccount[]>(`/api/aps/accounts`);
    }
    public static GetApsProjects(){
        return SimpleGet<IProject[]>(`/api/aps/projects`);
    }
    public static GetApsTopFolders({project}: {project: Project}){
        return SimpleGet<ITreeBase[]>(`/api/aps/accounts/${project.hubId}/projects/${project.projectId}/topFolders`);
    }
    public static GetApsFolder({folder}: {folder: Folder}){
        return SimpleGet<(ITreeBase | IFile)[]>(`/api/aps/projects/${folder.project.projectId}/folders/${folder.id}`);
    }
    //Schedules
    public static GetProjectSchedule({project}: {project: Project}){
        return SimpleGet<ISchedule[]>(`/api/projects/${project.projectId}/schedules`);
    }
    public static PostProjectSchedule({schedule}: {schedule: Schedule}){
        return SimplePost<ISchedule>(`/api/projects/${schedule.project.projectId}/schedules`, schedule.toInterface());
    }
    public static PatchProjectSchedule({schedule}: {schedule: Schedule}){
        return SimplePatch<ISchedule>(`/api/projects/${schedule.project.projectId}/schedules/${schedule.id}`, schedule.toInterface());
    }
    public static DeleteProjectSchedule({schedule}: {schedule: Schedule}){
        return SimpleDelete<void>(`/api/projects/${schedule.project.projectId}/schedules/${schedule.id}`, schedule.toInterface());
    }
    //Conversions
    public static PostConversion({project, files, folderUrns, ifcSettingsName}: {project: Project, files: IFileToUpload[], folderUrns: string[], ifcSettingsName: string}){
        return SimplePost<string>(`/api/projects/${project.projectId}/conversions`, {files, folderUrns, ifcSettingsName});
    }
    public static GetConversionJobs({project}: {project: Project}){
        return SimpleGet<IConversionJob[]>(`/api/projects/${project.projectId}/conversions?offset=${project.offset}&limit=${project.limit}`);
    }
    public static GetScheduleConversionJobs({projectId, scheduleId, offset, limit}: {projectId: string, scheduleId: string, offset: number, limit: number}){
        return SimpleGet<IConversionJob[]>(`/api/projects/${projectId}/schedules/${scheduleId}/conversions?offset=${offset}&limit=${limit}`);
    }
}



function SimplePost<T>(url: string, body?: any, options?: any): Promise<T> {
    return new Promise((resolve, reject) => {
        console.log("POST BODY", body)
        axios.post(url, body, options)
            .then(result => {
                resolve(result?.data)
            })
            .catch(error => {
                Error.create({
                    title: `Could not POST ${url}`,
                    message: `${error.response?.status}: ${error.response?.statusText} (${url})`,
                    appState
                });
                reject(error);
            })
    })
}

function SimplePatch<T>(url: string, body?: any, options?: any): Promise<T> {
    return new Promise((resolve, reject) => {
        axios.patch(url, body, options)
            .then(result => {
                resolve(result?.data)
            })
            .catch(error => {
                Error.create({
                    title: `Could not PATCH ${url}`,
                    message: `${error.response?.status}: ${error.response?.statusText} (${url})`,
                    appState
                });
                reject(error);
            })
    })
}

function SimpleDelete<T>(url: string, options?: any): Promise<T> {
    return new Promise((resolve, reject) => {
        axios.delete(url, options)
            .then(result => {
                resolve(result?.data)
            })
            .catch(error => {
                Error.create({
                    title: `Could not DELETE ${url}`,
                    message: `${error.response?.status}: ${error.response?.statusText} (${url})`,
                    appState
                });
                reject(error);
            })
    })
}

function  SimpleGet<T>(url: string, options?: any): Promise<T>{
    return new Promise((resolve, reject) => {
        axios.get(url, options)
            .then(result => {
                resolve(result?.data)
            })
            .catch(error => {
                Error.create({
                    title: `Could not GET ${url}`,
                    message: `${error.response?.status}: ${error.response?.statusText} (${url})`,
                    appState
                });
                reject(error);
            })
    })
}