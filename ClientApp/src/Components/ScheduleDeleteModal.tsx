import React from 'react';
import {observer} from "mobx-react-lite";
import {DefaultButton, Dialog, DialogFooter, DialogType, PrimaryButton} from "@fluentui/react";
import {Schedule} from "../Utilities/DataTypes/Schedule";
import { useTranslation } from 'react-i18next';

export const ScheduleDeleteModal = observer(({schedule, show, setShow}: {schedule: Schedule, show: boolean, setShow: Function})=>{

    const {t} = useTranslation();

    function deleteSchedule(){
        schedule
            .delete()
            .then(()=>setShow(false))
    }

    const dialogContentProps = {
        type: DialogType.normal,
        title: t('Delete Schedule'),
        closeButtonAriaLabel: 'Close'
    };

    return (
        <Dialog
            hidden={!show}
            onDismiss={()=>setShow(false)}
            dialogContentProps={dialogContentProps}
        >
            <div>
                {t(`Do you want to delete this schedule? This cannot be undone`)}
                <p style={{fontWeight: 'bold', textAlign: 'center', margin: '40px auto'}}>{schedule.name}</p>
            </div>

            <DialogFooter>
                    <PrimaryButton style={{float: 'left'}} onClick={deleteSchedule} className={'deleteButton'} text={t("Delete")} />
                    <DefaultButton onClick={()=>setShow(false)} text={t("Cancel")} />
            </DialogFooter>
        </Dialog>
    )
})