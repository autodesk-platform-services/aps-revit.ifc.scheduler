import {action, computed, makeObservable, observable, runInAction} from "mobx";
import {ApiCalls} from "../ApiCalls";

export interface IAccount{
    id: string;
    name: string;
    region: 'US' | 'EU'
    enabled: boolean
}

export class Account{
    public readonly id: string;
    public readonly name: string;
    public readonly region: 'US' | 'EU';
    @observable public enabled: boolean;

    constructor({id, name, region, enabled}: IAccount) {
        makeObservable(this);
        this.id = id;
        this.name = name;
        this.region = region;
        this.enabled = enabled;
    }

    @action.bound public toggleEnabled(){
        if(this.enabled){
            ApiCalls.DeleteAccount({account: this})
                .then(()=>runInAction(()=>this.enabled = false))
        } else {
            ApiCalls.PostAccount({account: this})
                .then(()=>runInAction(()=>this.enabled = true))
        }
    }

    @computed public get searchTerm(){return `${this.name}`.toLowerCase()}
}