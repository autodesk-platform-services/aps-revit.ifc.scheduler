import React, {useMemo, useState} from 'react';
import {observer} from 'mobx-react-lite';
import {DefaultButton, PrimaryButton} from "office-ui-fabric-react";
import CheckboxTree from 'react-checkbox-tree';
import 'react-checkbox-tree/lib/react-checkbox-tree.css';
import {useTranslation} from "react-i18next";
import {Project} from "../Utilities/DataTypes/Project";
import {Loading} from "./Loading";
import {Folder} from "../Utilities/DataTypes/Folder";
import {TreeBase} from "../Utilities/DataTypes/TreeBase";

interface IProjectFoldersAndFiles{
    project: Project;
    checked: string[];
    setChecked: Function;
    expanded: string[];
    setExpanded: Function;
}

export const ProjectFoldersAndFiles = observer(({project, checked, setChecked, expanded, setExpanded}: IProjectFoldersAndFiles)=>{
    const { t } = useTranslation();
    const [refreshMemo, setRefreshMemo] = useState(false);
    const [loading, setLoading] = useState(false);

    const nodes = useMemo(()=>{
        return project.folders.map(val=>val.getTree());
        //eslint-disable-next-line
    }, [project.foldersHash, project.folders, checked, refreshMemo])

    const fileCount = useMemo(()=>{
        if(Object.values(project.foldersHash).filter(val=>!val.fetched).length || !Object.keys(project.foldersHash).length){
            return t("Folders not yet expanded")
        } else {
            return checked
                .filter(val=>val.indexOf('urn:adsk.wipprod:fs.file:') > -1 && val.indexOf('_placeholderChild') === -1)
                .reduce((acc: string[], val)=>{
                    return acc.indexOf(val) === -1 ? acc.concat([val]) : acc;
                }, [])
                .length
        }
        /* Ignore warning to include 't' in the array */
        //eslint-disable-next-line
    }, [checked, project.foldersHash])

    function expandFolder(expanded: string[]){
        return new Promise<void>((resolve)=>{
            setExpanded(expanded);

            Promise.all(
                //Fetch all unfetched folders that have been expanded
                expanded
                    .map(val=>project.foldersHash[val])
                    .filter(val=>val && !val.fetched)
                    .map(val=>val.fetch())
            )
                .then(multipleFolderContents=>{
                    return multipleFolderContents.reduce((acc: TreeBase[], val)=>{
                        return acc.concat(val);
                    }, [])
                })
                .then((contents)=>{
                    const tempChecked = contents
                        .filter(val=>val.parent && checked.indexOf(val.parent.id) > -1)
                        .map(val=>val instanceof Folder ? [val.id, val.id + "_placeholderChild"] : [val.id])
                        .reduce((acc, val)=>acc.concat(val), [])
                        .concat(checked);
                    setChecked(tempChecked);
                })
                .finally(()=>{
                    setRefreshMemo(!refreshMemo);
                    resolve();
                })
        })
    }

    function selectAll(force?: boolean){
        if(force !== undefined){
            setChecked(force ? project.getAllIds() : [])
        } else if(checked.length === 0){
            setChecked(project.getAllIds())
        } else {
            setChecked([])
        }
    }

    function expandAllFolders(){
        setLoading(true);
        project.expandAllFolders()
            .then(()=>{
                selectAll(true);
                setLoading(false);
            });
    }

    function checkNode(checked: string[]){
        setChecked(checked);
    }

    return <div id={"projectFoldersAndFiles"}>
        <header>
            <PrimaryButton onClick={()=>selectAll()} text={t("Select All")}/>
            <div className={"flexFiller"}/>
            <div style={{width: '20rem'}}>{t("File Count")}: {fileCount}</div>
            <DefaultButton text={t("Count All Files")} onClick={expandAllFolders}/>
        </header>
        <hr style={{width: '100%'}}/>
        <div style={{overflowY: "auto"}}>
            {loading ? <div>
                <Loading/>
                {/*<h4>{project.folders.length} Folders Found...</h4>*/}
                {/*<h4>{project.files.length} Files Found...</h4>*/}
            </div> : project.fetched ?
                <CheckboxTree
                    checkModel={'all'}
                    nodes={nodes}
                    checked={checked}
                    expanded={expanded}
                    onCheck={checkNode}
                    onExpand={expandFolder}
                    iconsClass="fa5"
                />
                : <Loading/>
            }
        </div>
    </div>
})