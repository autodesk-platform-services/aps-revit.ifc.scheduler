import {IPermissions} from "./Permissions";
import {ApiCalls} from "../ApiCalls";

export interface IUser {
    name: string;
    permissions: IPermissions[]
}

export class User{
    public readonly permissions: IPermissions[];
    public readonly name: string;

    constructor({name, permissions}: IUser) {
        this.name = name;
        this.permissions = permissions;
    }

    public logout(){
        ApiCalls.logout().then(() => {
            window.location.reload();
        });
    }
}