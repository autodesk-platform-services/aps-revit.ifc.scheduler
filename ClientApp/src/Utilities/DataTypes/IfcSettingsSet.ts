import {computed, makeObservable, observable} from "mobx";

export interface IIfcSettingsSet {
    id?: string;
    name: string;
    isDefault: boolean
}

export class IfcSettingsSet{
    public readonly id?: string;
    @observable public name: string;
    @observable public isDefault: boolean;

    constructor({id, name, isDefault}: IIfcSettingsSet) {
        makeObservable(this);
        this.id = id;
        this.name = name;
        this.isDefault = isDefault;
    }

    @computed public get listValue(){
        return {
            label: this.name,
            value: this.name
        }
    }

    @computed public get searchTerm(){return `${this.name}`.toLowerCase()}
}