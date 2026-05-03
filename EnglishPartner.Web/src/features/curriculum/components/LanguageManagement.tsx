'use client'

import { useState } from 'react'
import { Plus, Loader2, Edit, Trash2, Globe } from 'lucide-react'
import { Button } from '@/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/ui/table'
import { 
  useLanguages, 
  useDeleteLanguage 
} from '../api/useCurriculum'
import type { LanguageResponse } from '../types/curriculum.types'
import { ConfirmActionDialog } from '@/shared/components/ConfirmActionDialog'
import { LanguageFormDialog } from './LanguageFormDialog'

export function LanguageManagement() {
  const { data: languages, isLoading } = useLanguages()
  const { mutate: deleteLanguage } = useDeleteLanguage()

  const [formOpen, setFormOpen] = useState(false)
  const [selectedLanguage, setSelectedLanguage] = useState<LanguageResponse | null>(null)
  
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false)
  const [languageToDelete, setLanguageToDelete] = useState<string | null>(null)

  const handleEdit = (language: LanguageResponse) => {
    setSelectedLanguage(language)
    setFormOpen(true)
  }

  const confirmDelete = async () => {
    if (languageToDelete) {
      await deleteLanguage(languageToDelete)
    }
    setDeleteConfirmOpen(false)
    setLanguageToDelete(null)
  }

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex justify-between items-center p-4 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-lg shadow-sm">
        <h2 className="text-lg font-semibold text-[var(--color-text-primary)] flex items-center gap-2">
          <Globe className="h-5 w-5 text-[var(--color-primary)]" />
          Supported Languages
        </h2>

        <Button 
          onClick={() => { setSelectedLanguage(null); setFormOpen(true) }}
          className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-white"
        >
          <Plus className="mr-2 h-4 w-4" />
          Add Language
        </Button>
      </div>

      {/* Main Table */}
      <div className="border border-[var(--color-border)] rounded-md bg-[var(--color-surface)] shadow-sm overflow-hidden">
        <Table>
          <TableHeader className="bg-[var(--color-surface-2)]">
            <TableRow className="border-b-[var(--color-border)] hover:bg-transparent">
              <TableHead className="w-16 text-center">Code</TableHead>
              <TableHead>Name</TableHead>
              <TableHead>Native Name</TableHead>
              <TableHead className="w-24 text-center">Sort Order</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="h-32 text-center">
                  <div className="flex items-center justify-center text-[var(--color-text-secondary)]">
                    <Loader2 className="h-6 w-6 animate-spin mr-2" />
                    Loading languages...
                  </div>
                </TableCell>
              </TableRow>
            ) : languages && languages.length > 0 ? (
              languages.map(lang => (
                <TableRow key={lang.id} className="border-b-[var(--color-border)] hover:bg-[var(--color-surface-2)]">
                  <TableCell className="text-center">
                    <span className="font-mono text-xs font-semibold px-2 py-1 rounded bg-[var(--color-primary-dim)] text-[var(--color-primary)]">
                      {lang.code}
                    </span>
                  </TableCell>
                  <TableCell>
                    <p className="font-medium text-[var(--color-text-primary)]">{lang.name}</p>
                  </TableCell>
                  <TableCell>
                    <p className="text-[var(--color-text-secondary)]">{lang.nativeName}</p>
                  </TableCell>
                  <TableCell className="text-center">
                    <span className="text-sm text-[var(--color-text-secondary)]">{lang.sortOrder}</span>
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex items-center justify-end gap-2">
                      <Button variant="ghost" size="sm" className="h-8 px-2 text-blue-500 hover:text-blue-600 hover:bg-blue-500/10" onClick={() => handleEdit(lang)}>
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="sm" className="h-8 px-2 text-red-500 hover:text-red-600 hover:bg-red-500/10" onClick={() => { setLanguageToDelete(lang.id); setDeleteConfirmOpen(true) }}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={5} className="h-32 text-center text-[var(--color-text-muted)]">
                  No languages found.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <LanguageFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        language={selectedLanguage}
      />

      <ConfirmActionDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title="Delete Language"
        description="Are you sure you want to delete this language? This action cannot be undone."
        confirmLabel="Delete"
        isDestructive={true}
        onConfirm={confirmDelete}
      />
    </div>
  )
}
